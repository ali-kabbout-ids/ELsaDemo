using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using Microsoft.AspNetCore.Mvc;

namespace PurchaseOrderApi.Controllers;

[ApiController, Route("api/debug")]
public class WorkflowDebugController(
    IWorkflowInstanceStore instances,
    IBookmarkStore bookmarks) : ControllerBase
{
    [HttpGet("instances")]
    public async Task<IActionResult> GetInstances(CancellationToken ct)
    {
        IEnumerable<Elsa.Workflows.Management.Entities.WorkflowInstance> all = await instances.FindManyAsync(new WorkflowInstanceFilter(), ct);
        return Ok(all.Select(i => new
        {
            i.Id, i.DefinitionId, i.CorrelationId,
            Status = i.Status.ToString(),
            i.CreatedAt, i.UpdatedAt, i.FinishedAt
        }));
    }

    [HttpGet("instances/{instanceId}")]
    public async Task<IActionResult> GetInstance(string instanceId, CancellationToken ct)
    {
        Elsa.Workflows.Management.Entities.WorkflowInstance? inst = await instances.FindAsync(
            new WorkflowInstanceFilter { Id = instanceId }, ct);
        if (inst is null) return NotFound();

        IEnumerable<Elsa.Workflows.Runtime.Entities.StoredBookmark> bms = await bookmarks.FindManyAsync(
            new BookmarkFilter { WorkflowInstanceId = instanceId }, ct);

        return Ok(new
        {
            inst.Id, inst.DefinitionId, inst.CorrelationId,
            Status    = inst.Status.ToString(),
            inst.CreatedAt, inst.UpdatedAt, inst.FinishedAt,
            Bookmarks = bms.Select(b => new { b.Id, b.ActivityTypeName, b.CreatedAt })
        });
    }

    [HttpGet("bookmarks")]
    public async Task<IActionResult> GetBookmarks(CancellationToken ct)
    {
        IEnumerable<Elsa.Workflows.Runtime.Entities.StoredBookmark> bms = await bookmarks.FindManyAsync(new BookmarkFilter(), ct);
        return Ok(bms.Select(b => new
            { b.Id, b.WorkflowInstanceId, b.ActivityTypeName, b.CreatedAt }));
    }
}
