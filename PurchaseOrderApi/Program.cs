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
    options.AddPolicy("ElsaStudioPolicy", policy =>
    {
        policy.WithOrigins("https://localhost:44314")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddDbContext<AppDbContext>(opts => opts.UseSqlServer(connStr));
builder.Services.AddScoped<PurchaseOrderService>();

// --- 3. ELSA SETUP ---
builder.Services.AddElsa(elsa =>
{
    elsa.UseWorkflowManagement(m => m.UseEntityFrameworkCore(ef =>
    {
        ef.UseSqlServer(connStr);
        ef.RunMigrations = builder.Environment.IsDevelopment();
    }));

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

    elsa.UseWorkflowsApi();
    elsa.UseFlowchart();
    elsa.UseResilience();

    // Activities
    elsa.AddActivity<ValidateOrderActivity>();
    elsa.AddActivity<NotifyManagerActivity>();
    elsa.AddActivity<WaitForApprovalActivity>();
    elsa.AddActivity<ApproveOrderActivity>();
    elsa.AddActivity<RejectOrderActivity>();

    elsa.AddWorkflow<PurchaseOrderApprovalWorkflow>();
});

if (builder.Environment.IsDevelopment())
{
    EndpointSecurityOptions.DisableSecurity();
}

var app = builder.Build();


app.UseRouting();
app.UseCors("ElsaStudioPolicy");

app.UseWorkflows();

app.UseWorkflowsApi();


app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Purchase Order API v1");
});

if (builder.Environment.IsProduction())
{
    app.UseAuthentication();
    app.UseAuthorization();
}

// Database Auto-Migration
if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await appDb.Database.MigrateAsync();
}

app.MapControllers();
app.Run();