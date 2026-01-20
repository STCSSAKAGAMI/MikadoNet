using MikadoNet.Dtos;

namespace MikadoNet.Services;

public interface IHaiinService
{
    Task<int> SaveDraftAsync(HaiinDraftRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<short>> ConfirmAsync(HaiinConfirmRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<HaiinCalendarEntry>> GetCalendarAsync(HaiinCalendarRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkRequestDto>> GetSagyouAsync(HaiinDataLoadRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<HaiinRecordDto>> GetHaiinAsync(HaiinDataLoadRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<TantoDto>> GetTantoAsync(CancellationToken cancellationToken);
    Task<DateTime?> GetHaiinDateAsync(CancellationToken cancellationToken);
    Task<DateTime?> GetLastEditHaiinAsync(CancellationToken cancellationToken);
    Task<DateTime?> GetLastSendMailAsync(CancellationToken cancellationToken);
    Task UpdateHaiinDateAsync(DateTime haiinDate, CancellationToken cancellationToken);
    Task<HaiinMailBatch> GetMailBatchAsync(DateTime? since, CancellationToken cancellationToken);
    Task MarkMailSentAsync(DateTime sentAt, DateTime? since, CancellationToken cancellationToken);
    Task UpdateSagyouConfirmedAtAsync(int workRequestCode, DateTime confirmedAt, CancellationToken cancellationToken);
    Task<HaiinNotificationInfo?> GetNotificationInfoAsync(HaiinNotificationRequest request, CancellationToken cancellationToken);
    Task<WorkRequestSummary?> GetWorkRequestAsync(int workRequestCode, CancellationToken cancellationToken);
    Task<SupervisorInfo?> GetSupervisorInfoAsync(int assigneeCode, CancellationToken cancellationToken);
}
