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

[ApiController]
[Route("api/applications")]
public class ApplicationsController(
    ApplicationService svc,
    IWorkflowRuntime workflowRuntime,
    IWorkflowDispatcher workflowDispatcher) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await svc.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        PurchaseOrderApi.Models.ApplicationRequest? app = await svc.GetByIdAsync(id);
        return app is null ? NotFound() : Ok(app);
    }

    [HttpPost]
    public async Task<IActionResult> Start([FromBody] StartApplicationRequest req)
    {
        PurchaseOrderApi.Models.ApplicationRequest app = await svc.CreateAsync(req);
        IWorkflowClient client = await workflowRuntime.CreateClientAsync();

        RunWorkflowInstanceResponse result = await client.CreateAndRunInstanceAsync(new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(
                ApplicationRequestWorkFlow.DefinitionId),
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

    [HttpPost("{id:int}/i3lam-kanouni/decide")]
    public Task<IActionResult> I3lamKanouniDecide(int id, [FromBody] ReviewDecisionRequest req)
        => Resume<WaitForI3almKanouniActivity>(id, req);

    [HttpPost("{id:int}/mo3awen/decide")]
    public Task<IActionResult> Mo3awenDecide(int id, [FromBody] ReviewDecisionRequest req)
        => Resume<WaitForMo3awenCho3baActivity>(id, req);

    [HttpPost("{id:int}/mo5atabat/decide")]
    public Task<IActionResult> Mo5atabatDecide(int id, [FromBody] ReviewDecisionRequest req)
        => Resume<WaitForMo5atabatActivity>(id, req);

    [HttpPost("{id:int}/final-mo3awen/decide")]
    public Task<IActionResult> FinalMo3awenDecide(int id, [FromBody] ReviewDecisionRequest req)
        => Resume<WaitForFinalMo3awenActivity>(id, req);

    [HttpPost("{id:int}/has-mane3/decide")]
    public Task<IActionResult> HasMane3Decide(int id, [FromBody] ReviewDecisionRequest req)
        => Resume<WaitForHasMane3AnouniActivity>(id, req);

    private async Task<IActionResult> Resume<TActivity>(int appId, ReviewDecisionRequest req)
        where TActivity : IActivity
    {
        ApplicationRequest? app = await svc.GetByIdAsync(appId);
        if (app is null) return NotFound();

        string activityTypeName = ActivityTypeNameHelper.GenerateTypeName<TActivity>();
        string stimulus = appId.ToString();
        DispatchTriggerWorkflowsRequest request = new DispatchTriggerWorkflowsRequest(activityTypeName, stimulus)
        {
            CorrelationId = appId.ToString(),
            Input = new Dictionary<string, object>
            {
                ["decision"] = req.Decision,
                ["reason"] = req.Reason ?? string.Empty
            }
        };

        DispatchWorkflowOptions options = new DispatchWorkflowOptions
        {
            Channel = null // Uses the default processing channel
        };

        await workflowDispatcher.DispatchAsync(request, options);

        return Ok(await svc.GetByIdAsync(appId));
    }
}