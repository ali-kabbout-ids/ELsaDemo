using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Parameters;
using Microsoft.AspNetCore.Mvc;
using PurchaseOrderApi.Models;
using PurchaseOrderApi.Services;
using PurchaseOrderApi.Workflows;

namespace PurchaseOrderApi.Controllers;

[ApiController, Route("api/orders")]
public class PurchaseOrdersController(
    PurchaseOrderStore store,
    IWorkflowRuntime runtime,
    IWorkflowInstanceStore instanceStore,
    IBookmarkStore bookmarkStore) : ControllerBase
{
    [HttpPost]
    public IActionResult Create([FromBody] CreateOrderRequest req)
    {
        var order = store.Create(req);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpGet]
    public IActionResult GetAll() => Ok(store.GetAll());

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
        => store.GetById(id) is {} order ? Ok(order) : NotFound();

    // Submit → Start Elsa Workflow
    [HttpPost("{id}/submit")]
    public async Task<IActionResult> Submit(int id, CancellationToken ct)
    {
        var order = store.GetById(id);
        if (order is null)                         return NotFound($"PO #{id} not found.");
        if (order.Status != OrderStatus.Draft)
            return BadRequest($"PO #{id} is '{order.Status}'. Only Draft orders can be submitted.");

        var result =  await runtime.StartWorkflowAsync(
            PurchaseOrderApprovalWorkflow.DefinitionId,
            new StartWorkflowRuntimeParams
            {
                CorrelationId = id.ToString(),
                Input = new Dictionary<string, object> { ["orderId"] = id },
                CancellationToken = ct
            });

        order.WorkflowInstanceId = result.WorkflowInstanceId;
        store.Save(order);

        return Accepted(new
        {
            message            = $"Workflow started for PO #{id}. Awaiting manager approval.",
            workflowInstanceId = result.WorkflowInstanceId,
            correlationId      = id.ToString()
        });
    }

    // Decide → Resume Elsa Workflow
    [HttpPost("{id}/decide")]
    public async Task<IActionResult> Decide(
        int id,
        [FromBody] ApprovalDecisionRequest req,
        CancellationToken ct)
    {
        var order = store.GetById(id);
        if (order is null)
            return NotFound($"PO #{id} not found.");

        if (order.Status != OrderStatus.PendingApproval)
            return BadRequest($"PO #{id} is not pending approval (status: {order.Status}).");

        var decision = req.Decision.ToLower();
        if (decision != "approved" && decision != "rejected")
            return BadRequest("Decision must be 'approved' or 'rejected'.");

        var instance = (await instanceStore.FindManyAsync(
            new WorkflowInstanceFilter { CorrelationId = id.ToString() }, ct))
            .FirstOrDefault(i => i.SubStatus == WorkflowSubStatus.Suspended);

        if (instance is null)
            return NotFound($"No suspended workflow for PO #{id}.");

        var bookmarks = await bookmarkStore.FindManyAsync(
            new BookmarkFilter { WorkflowInstanceId = instance.Id }, ct);

        var bookmark = bookmarks.FirstOrDefault(b =>
            b.ActivityTypeName == "UserTask" // 🔥 adjust to your workflow
        );

        if (bookmark is null)
            return NotFound("Expected bookmark not found.");

        await runtime.ResumeWorkflowAsync(
            instance.Id,
            new ResumeWorkflowRuntimeParams
            {
                BookmarkId = bookmark.Id,
                Input = new Dictionary<string, object>
                {
                    ["decision"] = decision,
                    ["reason"] = req.Reason ?? string.Empty
                },
                CancellationToken = ct
            });

        return Ok(new
        {
            message = $"Decision '{decision}' recorded. Workflow resumed.",
            workflowInstanceId = instance.Id
        });
    }
}
