using Microsoft.AspNetCore.Mvc;
using PurchaseOrderApi.Dtos;
using PurchaseOrderApi.Services;

namespace PurchaseOrderApi.Controllers;

[ApiController]
[Route("api/workflow-inbox")]
public sealed class WorkflowInboxController(WorkflowInboxService inboxService) : ControllerBase
{
    /// <summary>
    /// Returns pending workflow inbox items for the specified role.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<InboxItemDto>>> GetPending([FromQuery] string role)
        => Ok(await inboxService.GetPendingItemsAsync(role));

    /// <summary>
    /// Returns a single workflow inbox item for the specified bookmark ID.
    /// </summary>
    [HttpGet("{bookmarkId}")]
    public async Task<ActionResult<InboxItemDto>> GetByBookmarkId([FromRoute] string bookmarkId)
    {
        List<InboxItemDto> all = await inboxService.GetPendingItemsAsync(string.Empty);
        InboxItemDto? item = all.FirstOrDefault(x => x.BookmarkId == bookmarkId);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>
    /// Submits a decision for the specified bookmark and resumes the workflow.
    /// </summary>
    [HttpPost("{bookmarkId}/submit")]
    public async Task<IActionResult> Submit(
        [FromRoute] string bookmarkId,
        [FromBody] SubmitDecisionRequest request,
        [FromQuery] string role)
    {
        List<InboxItemDto> all = await inboxService.GetPendingItemsAsync(string.Empty);
        InboxItemDto? item = all.FirstOrDefault(x => x.BookmarkId == bookmarkId);
        if (item is null) return NotFound();

        if (!string.Equals(item.RequiredRole, role, StringComparison.OrdinalIgnoreCase))
            return StatusCode(403);

        await inboxService.SubmitDecisionAsync(bookmarkId, request.Decision, request.Reason);
        return Ok();
    }
}

