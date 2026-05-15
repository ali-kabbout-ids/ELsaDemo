using Elsa.Workflows.Helpers;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime;
using Microsoft.AspNetCore.Mvc;
using PurchaseOrderApi.Activities;
using PurchaseOrderApi.Dtos;
using PurchaseOrderApi.Services;
using PurchaseOrderApi.Workflows;
using Elsa.Workflows;
using Elsa.Workflows.Runtime.Requests;
using PurchaseOrderApi.Models;
using PurchaseOrderApi.Activities.ApplicationActivities.MokhatabatActivities;
using Elsa.Common.Models;

[ApiController]
[Route("api/applications")]
public class ApplicationsController(
    ApplicationService svc,
    MokhatabatService mokhatabatService,
    IWorkflowRuntime workflowRuntime,
    IWorkflowDispatcher workflowDispatcher) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await svc.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        ApplicationRequest? app = await svc.GetByIdAsync(id);
        return app is null ? NotFound() : Ok(app);
    }

    [HttpPost]
    public async Task<IActionResult> Start([FromBody] StartApplicationRequest req)
    {
        ApplicationRequest app = await svc.CreateAsync(req);
        IWorkflowClient client = await workflowRuntime.CreateClientAsync();

        RunWorkflowInstanceResponse result = await client.CreateAndRunInstanceAsync(new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(
                ApplicationRequestWorkFlow.DefinitionId, VersionOptions.Published),
            CorrelationId = app.Id.ToString(), 
            Input = new Dictionary<string, object>
            {
                ["applicationId"] = app.Id,
                ["requiresMo5atabat"] = req.RequiresMo5atabat
            }
        });

        app.WorkflowInstanceId = result.WorkflowInstanceId;
        await svc.SaveAsync(app);
        return CreatedAtAction(nameof(GetById), new { id = app.Id }, app);
    }

    // ── Decision endpoints ───────────────────────────────────────────────

    [HttpPost("{id:int}/decide")]
    public async Task<IActionResult> Decide(int id, [FromBody] ReviewDecisionRequest req)
    {
        var app = await svc.GetByIdAsync(id);
        if (app is null)
            return NotFound($"Application #{id} not found.");

        // ── Role check ────────────────────────────────────────────────────────
        if (string.IsNullOrEmpty(app.CurrentRequiredRole))
            return BadRequest("This application is not waiting for any decision right now.");

        if (!string.Equals(app.CurrentRequiredRole, req.UserRole, StringComparison.OrdinalIgnoreCase))
            return StatusCode(403, new
            {
                error = "Role mismatch",
                message = $"Step '{app.CurrentStepName}' requires role '{app.CurrentRequiredRole}'. " +
                          $"You submitted as '{req.UserRole}'."
            });

        string? activityTypeName = ActivityTypeNameHelper.GenerateTypeName<WaitForApplicationApprovalActivity>();

        await workflowDispatcher.DispatchAsync(
            new DispatchTriggerWorkflowsRequest(activityTypeName, id.ToString())
            {
                CorrelationId = id.ToString(),
                Input = new Dictionary<string, object>
                {
                    ["decision"] = req.Decision,
                    ["reason"] = req.Reason ?? string.Empty,
                    ["userRole"] = req.UserRole           
                }
            },
            new DispatchWorkflowOptions());

        return Ok(await svc.GetByIdAsync(id));
    }

    [HttpGet("{id:int}/has-mane3")]
    public async Task<IActionResult> HasMane3Check(
        int id,
        [FromQuery] string decision = "no_obstacle",
        [FromQuery] string reason = "")
    {
        var app = await svc.GetByIdAsync(id);
        if (app is null) return NotFound($"Application #{id} not found.");

        return Ok(new
        {
            decision = decision.ToLower(),
            reason = reason
        });
    }


    [HttpGet("{id:int}/mo5atabat")]
    public async Task<IActionResult> GetMo5atabat(int id)
    {
        MokhatabatRequest? record = await mokhatabatService.GetByApplicationIdAsync(id);
        return record is null ? NotFound() : Ok(record);
    }

    [HttpPost("{id:int}/mo5atabat/step1/decide")]
    public Task<IActionResult> Mo5atabatStep1Decide(int id, [FromBody] ReviewDecisionRequest req)
        => ResumeMo5<MokhatabatStep1Activity>(id, req);

    [HttpPost("{id:int}/mo5atabat/step2/decide")]
    public Task<IActionResult> Mo5atabatStep2Decide(int id, [FromBody] ReviewDecisionRequest req)
        => ResumeMo5<MokhatabatStep2Activity>(id, req);

    private async Task<IActionResult> ResumeMo5<TActivity>(int appId, ReviewDecisionRequest req)
    where TActivity : IActivity
    {
        string activityTypeName = ActivityTypeNameHelper.GenerateTypeName<TActivity>();
        string stimulus = appId.ToString();

        DispatchTriggerWorkflowsRequest request = new DispatchTriggerWorkflowsRequest(activityTypeName, stimulus)
        {
            CorrelationId = $"mo5-{appId}", 
            Input = new Dictionary<string, object>
            {
                ["decision"] = req.Decision,
                ["reason"] = req.Reason ?? string.Empty
            }
        };

        await workflowDispatcher.DispatchAsync(request, new DispatchWorkflowOptions());
        return Ok(await svc.GetByIdAsync(appId));
    }
}