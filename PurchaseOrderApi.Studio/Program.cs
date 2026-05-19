using Elsa.Studio.Dashboard.Extensions;
using Elsa.Studio.Shell;
using Elsa.Studio.Shell.Extensions;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Core.BlazorWasm.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Workflows.Designer.Extensions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using PurchaseOrderApi.Studio.Providers;
using Elsa.Studio.Login.Services;
using Elsa.Studio.Labels;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor;
using Elsa.Studio.Labels.Contracts;
using Elsa.Studio.WorkflowContexts.Services;
using Elsa.Studio.WorkflowContexts.Widgets;
using Elsa.Studio.WorkflowContexts.Extensions;
using PurchaseOrderApi.Studio.CustomWidgets;

// Build the host.
WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
WebAssemblyHostConfiguration configuration = builder.Configuration;

// Register root components.
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.RootComponents.RegisterCustomElsaStudioElements();

// Register shell services and modules.
BackendApiConfig backendApiConfig = new BackendApiConfig
{
    ConfigureBackendOptions = options => builder.Configuration.GetSection("Backend").Bind(options)
};

builder.Services.AddCore();
builder.Services.AddShell();
builder.Services.AddSingleton<IAuthenticationProviderManager, DefaultAuthenticationProviderManager>();
builder.Services.AddRemoteBackend(backendApiConfig);
builder.Services.AddDashboardModule();
builder.Services.AddWorkflowsModule();
builder.Services.AddScoped<AuthenticationStateProvider, AuthStateProvider>();
builder.Services.AddAuthorizationCore();
//builder.Services.AddLabelsModule(backendApiConfig);
builder.Services.AddWorkflowContextsModule();
//builder.Services.AddScoped<IWorkflowDefinitionLabelsProvider, RemoteWorkflowDefinitionLabelsProvider>();
//builder.Services.AddScoped<IWidget, WorkflowDefinitionLabelsEditorWidget>();
builder.Services.AddScoped<IWidget, TransactionTypeEditorWidget>();

// Build the application.
var app = builder.Build();

// Run each startup task.
var startupTaskRunner = app.Services.GetRequiredService<IStartupTaskRunner>();
await startupTaskRunner.RunStartupTasksAsync();

// Run the application.
await app.RunAsync();