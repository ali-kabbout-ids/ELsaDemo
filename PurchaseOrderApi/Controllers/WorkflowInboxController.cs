using Microsoft.AspNetCore.Mvc;
using PurchaseOrderApi.Dtos;
using PurchaseOrderApi.Enums;
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
        [FromBody] SubmitActionRequest request,
        [FromQuery] string role)
    {
        bool isValid = await inboxService.ValidateRoleAsync(bookmarkId, role);
        if (!isValid)
            return StatusCode(403, new { error = "You do not have permission to action this task." });

        if (!WorkflowActions.IsValid(request.Action))
            return BadRequest(new { error = $"Unknown action '{request.Action}'." });

        try
        {
            await inboxService.SubmitDecisionAsync(bookmarkId, request.Action, request.Reason, request.Extra);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
    }
}

