using Elsa;
using Elsa.Extensions;
using Microsoft.EntityFrameworkCore;
using PurchaseOrderApi.Activities;
using PurchaseOrderApi.Data;
using PurchaseOrderApi.Services;
using PurchaseOrderApi.Workflows;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Resilience.Extensions;
using PurchaseOrderApi.Activities.ApplicationActivities.MokhatabatActivities;
using PurchaseOrderApi.Providers;
using Elsa.Workflows;
using PurchaseOrderApi.Dtos;
using Elsa.Persistence.EFCore.Modules.Labels;
using Elsa.Identity.Contracts;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string connStr = builder.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException("Connection string 'Database' is missing.");

string elsaHttpBaseUrl = builder.Configuration.GetValue<string>("Elsa:Http:BaseUrl")
    ?? "https://localhost:44306";

// --- 2. SERVICES ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Purchase Order API — Elsa Demo", Version = "v1" });

    c.CustomSchemaIds(x => x.FullName);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<AppDbContext>(opts => opts.UseSqlServer(connStr));
builder.Services.AddScoped<PurchaseOrderService>();
builder.Services.AddScoped<ApplicationService>();
builder.Services.AddScoped<MokhatabatService>();
builder.Services.AddScoped<WorkflowInboxService>();
builder.Services.AddScoped<ApprovalRoleSelectListProvider>();
builder.Services.AddScoped<IPropertyUIHandler, WorkflowActionUIProvider>();
builder.Services.AddScoped<IPropertyUIHandler, SectionUIProvider>();
builder.Services.AddHttpClient("platzi");
builder.Services.AddScoped<WorkflowService>();

var signingKey = builder.Configuration["Identity:Tokens:SigningKey"];

if (string.IsNullOrWhiteSpace(signingKey) || signingKey.Length < 32)
{
    throw new InvalidOperationException("Security Error: The Identity:Tokens:SigningKey must be configured in appsettings.json and be at least 32 characters long.");
}

//builder.Services.AddScoped<ITransactionWorkflowResolver, TransactionWorkflowResolver>();
//builder.Services.AddHostedService<WorkflowTransactionTypeSeeder>();

// --- 3. ELSA SETUP ---
builder.Services.AddElsa(elsa =>
{
    //elsa.UseLabels();
    //elsa.UseLabels(labels => labels.UseEntityFrameworkCore(ef =>
    //{
    //    ef.DbContextOptionsBuilder = (_, opts) => opts
    //        .UseSqlServer(connStr, sql =>
    //            sql.MigrationsAssembly("Elsa.Persistence.EFCore.SqlServer"))
    //        .ReplaceService<IMigrationsAssembly, DbSchemaAwareMigrationAssembly>();
    //}));

    elsa.UseWorkflowManagement(m =>
    {
        m.UseEntityFrameworkCore(ef =>
        {
            ef.UseSqlServer(connStr);
            ef.RunMigrations = builder.Environment.IsDevelopment();
        });
    });

    elsa.UseWorkflowRuntime(r => r.UseEntityFrameworkCore(ef =>
    {
        ef.UseSqlServer(connStr);
        ef.RunMigrations = builder.Environment.IsDevelopment();
    }));

    elsa.UseHttp(http =>
    {
        http.ConfigureHttpOptions = options =>
        {
            options.BaseUrl = new Uri(elsaHttpBaseUrl);
        };
    });

    elsa.AddVariableTypeAndAlias<ApprovalResult>("ApprovalResult", "Application");

    elsa.UseWorkflowsApi();
    elsa.UseFlowchart();
    elsa.UseResilience();

    var identitySection = builder.Configuration.GetSection("Identity");
    var tokenSection = identitySection.GetSection("Tokens");

    elsa.UseIdentity(identity =>
    {
        identity.TokenOptions = opts =>
        {
            tokenSection.Bind(opts);
            opts.Issuer = elsaHttpBaseUrl;
            opts.Audience = elsaHttpBaseUrl;
        };

        identity.UseConfigurationBasedRoleProvider(options =>
        {
            identitySection.Bind(options);
        });
    });
    elsa.UseDefaultAuthentication();
    elsa.UseLabels();
     elsa.UseJavaScript();

    // Activities
    elsa.AddActivity<ValidateOrderActivity>();
    elsa.AddActivity<NotifyManagerActivity>();
    elsa.AddActivity<WaitForApprovalActivity>();
    elsa.AddActivity<ApproveOrderActivity>();
    elsa.AddActivity<RejectOrderActivity>();

    elsa.AddActivity<WaitForApplicationApprovalActivity>();

    elsa.AddActivity<MokhatabatStep1Activity>();
    elsa.AddActivity<MokhatabatStep2Activity>();

    elsa.AddWorkflow<PurchaseOrderApprovalWorkflow>();
    elsa.AddWorkflow<ApplicationRequestWorkFlow>();
    elsa.AddWorkflow<MokhatabatWorkflow>();
});

builder.Services.AddScoped<IUserCredentialsValidator, PlatziUserCredentialsValidator>();


//if (builder.Environment.IsDevelopment())
//{
//    EndpointSecurityOptions.DisableSecurity();
//}

WebApplication app = builder.Build();


app.UseRouting();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.UseWorkflows();

app.UseWorkflowsApi();


app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Purchase Order API v1");
});



// Database Auto-Migration
if (app.Environment.IsDevelopment())
{
    await using AsyncServiceScope scope = app.Services.CreateAsyncScope();

    await scope.ServiceProvider.GetRequiredService<AppDbContext>()
        .Database.MigrateAsync();

    //// Migrate LabelsElsaDbContext before seeding  
    //var labelsDbFactory = scope.ServiceProvider
    //    .GetRequiredService<IDbContextFactory<LabelsElsaDbContext>>();
    //await using var labelsDb = await labelsDbFactory.CreateDbContextAsync();
    //await labelsDb.Database.MigrateAsync();

    //// Now seed safely  
    //var labelStore = scope.ServiceProvider.GetRequiredService<ILabelStore>();
    //var existing = (await labelStore.ListAsync()).Items.Select(l => l.Name).ToHashSet();
    //foreach (var txType in TransactionTypes.All)
    //{
    //    if (!existing.Contains(txType))
    //        await labelStore.SaveAsync(new Label
    //        {
    //            Id = Guid.NewGuid().ToString(),
    //            Name = txType,
    //            Description = $"Workflows that handle {txType} transactions"
    //        });
    //}
}

app.MapControllers();
app.Run();