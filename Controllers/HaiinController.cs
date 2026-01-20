using Microsoft.AspNetCore.Mvc;
using MikadoNet.Dtos;
using MikadoNet.Services;

namespace MikadoNet.Controllers;

[ApiController]
[Route("api/haiin")]
public class HaiinController : ControllerBase
{
    private readonly IHaiinService _haiinService;

    public HaiinController(IHaiinService haiinService)
    {
        _haiinService = haiinService;
    }

    [HttpPost("draft")]
    public async Task<ActionResult<int>> SaveDraft([FromBody] HaiinDraftRequest request, CancellationToken cancellationToken)
    {
        var sequence = await _haiinService.SaveDraftAsync(request, cancellationToken);
        return Ok(sequence);
    }

    [HttpPost("confirm")]
    public async Task<ActionResult<IReadOnlyList<short>>> Confirm([FromBody] HaiinConfirmRequest request, CancellationToken cancellationToken)
    {
        var codes = await _haiinService.ConfirmAsync(request, cancellationToken);
        return Ok(codes);
    }

    [HttpPost("calendar")]
    public async Task<ActionResult<IReadOnlyList<HaiinCalendarEntry>>> Calendar([FromBody] HaiinCalendarRequest request, CancellationToken cancellationToken)
    {
        var entries = await _haiinService.GetCalendarAsync(request, cancellationToken);
        return Ok(entries);
    }

    [HttpPost("sagyou")]
    public async Task<ActionResult<IReadOnlyList<WorkRequestDto>>> GetSagyou([FromBody] HaiinDataLoadRequest request, CancellationToken cancellationToken)
    {
        var results = await _haiinService.GetSagyouAsync(request, cancellationToken);
        return Ok(results);
    }

    [HttpPost("records")]
    public async Task<ActionResult<IReadOnlyList<HaiinRecordDto>>> GetHaiin([FromBody] HaiinDataLoadRequest request, CancellationToken cancellationToken)
    {
        var results = await _haiinService.GetHaiinAsync(request, cancellationToken);
        return Ok(results);
    }

    [HttpGet("tanto")]
    public async Task<ActionResult<IReadOnlyList<TantoDto>>> GetTanto(CancellationToken cancellationToken)
    {
        var results = await _haiinService.GetTantoAsync(cancellationToken);
        return Ok(results);
    }

    [HttpGet("meta/haiin-date")]
    public async Task<ActionResult<DateTime?>> GetHaiinDate(CancellationToken cancellationToken)
    {
        var result = await _haiinService.GetHaiinDateAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("meta/haiin-date")]
    public async Task<IActionResult> UpdateHaiinDate([FromBody] HaiinDateRequest request, CancellationToken cancellationToken)
    {
        await _haiinService.UpdateHaiinDateAsync(request.HaiinDate, cancellationToken);
        return Ok();
    }

    [HttpGet("meta/last-edit")]
    public async Task<ActionResult<DateTime?>> GetLastEdit(CancellationToken cancellationToken)
    {
        var result = await _haiinService.GetLastEditHaiinAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("meta/last-send-mail")]
    public async Task<ActionResult<DateTime?>> GetLastSendMail(CancellationToken cancellationToken)
    {
        var result = await _haiinService.GetLastSendMailAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("mail/batch")]
    public async Task<ActionResult<HaiinMailBatch>> GetMailBatch([FromQuery] DateTime? since, CancellationToken cancellationToken)
    {
        var result = await _haiinService.GetMailBatchAsync(since, cancellationToken);
        return Ok(result);
    }

    [HttpPost("mail/mark-sent")]
    public async Task<IActionResult> MarkMailSent([FromBody] HaiinMailSentRequest request, CancellationToken cancellationToken)
    {
        await _haiinService.MarkMailSentAsync(request.SentAt, request.Since, cancellationToken);
        return Ok();
    }

    [HttpPost("mail/sagyou-confirmed")]
    public async Task<IActionResult> MarkSagyouConfirmed([FromBody] HaiinSagyouConfirmedRequest request, CancellationToken cancellationToken)
    {
        await _haiinService.UpdateSagyouConfirmedAtAsync(request.WorkRequestCode, request.ConfirmedAt, cancellationToken);
        return Ok();
    }

    [HttpPost("mail/notification")]
    public async Task<ActionResult<HaiinNotificationInfo?>> GetNotificationInfo([FromBody] HaiinNotificationRequest request, CancellationToken cancellationToken)
    {
        var result = await _haiinService.GetNotificationInfoAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("work-request/{workRequestCode:int}")]
    public async Task<ActionResult<WorkRequestSummary?>> GetWorkRequest(int workRequestCode, CancellationToken cancellationToken)
    {
        var result = await _haiinService.GetWorkRequestAsync(workRequestCode, cancellationToken);
        return Ok(result);
    }

    [HttpGet("supervisor/{assigneeCode:int}")]
    public async Task<ActionResult<SupervisorInfo?>> GetSupervisor(int assigneeCode, CancellationToken cancellationToken)
    {
        var result = await _haiinService.GetSupervisorInfoAsync(assigneeCode, cancellationToken);
        return Ok(result);
    }
}

public class HaiinViewController : Controller
{
    private readonly IHaiinService _haiinService;

    public HaiinViewController(IHaiinService haiinService)
    {
        _haiinService = haiinService;
    }

    [HttpGet("/haiin")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var assignees = await _haiinService.GetTantoAsync(cancellationToken);
        var model = new MikadoNet.Models.HaiinViewModel
        {
            Assignees = assignees
        };
        return View(model);
    }
}
