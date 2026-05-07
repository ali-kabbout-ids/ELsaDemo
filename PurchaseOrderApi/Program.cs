using Elsa.Extensions;
using PurchaseOrderApi.Activities;
using PurchaseOrderApi.Services;
using PurchaseOrderApi.Workflows;
using Elsa;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("ElsaStudioPolicy", policy =>
    {
        policy.WithOrigins("https://localhost:44314") // Explicitly allow Studio
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Crucial for Blazor/Elsa communication
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Purchase Order API — Elsa Demo", Version = "v1" });

    // This is crucial: Elsa and your API might have overlapping route names
    c.CustomSchemaIds(x => x.FullName);
});

// In-memory PO store (singleton = lives for app lifetime)
builder.Services.AddSingleton<PurchaseOrderStore>();

// ── ELSA SETUP ──────────────────────────────────────────────────────────────
builder.Services.AddElsa(elsa =>
{
    // 1. All state stored in memory — no DB, no migrations, zero config
    //    (in-memory is Elsa 3's default; no sub-config needed)
    elsa.UseWorkflowManagement();
    elsa.UseWorkflowRuntime();
    elsa.UseFlowchart();
    //elsa.UseHttp();
    // 2. REST API endpoints — Elsa Studio connects to these
    elsa.UseWorkflowsApi();


    // 3. Register custom activities so Elsa can resolve + inject DI services
    elsa.AddActivity<ValidateOrderActivity>();
    elsa.AddActivity<NotifyManagerActivity>();
    elsa.AddActivity<WaitForApprovalActivity>();
    elsa.AddActivity<ApproveOrderActivity>();
    elsa.AddActivity<RejectOrderActivity>();

    // 4. Register the workflow definition — Elsa loads it on startup
    elsa.AddWorkflow<PurchaseOrderApprovalWorkflow>();
});

if (builder.Environment.IsDevelopment())
{
    // The static call is usually on EndpointSecurityOptions directly 
    // if the Elsa.Common package is referenced.
    EndpointSecurityOptions.DisableSecurity();
}
// ────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseRouting(); // Ensure routing is enabled first

// Explicitly use the named policy
app.UseCors("ElsaStudioPolicy");
app.UseSwagger();

if (builder.Environment.IsProduction())
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Purchase Order API v1");
});
app.UseWorkflowsApi();  // maps Elsa REST routes (used by Studio)
app.MapControllers();

app.Run();
