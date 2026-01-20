using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MikadoNet.Data;
using MikadoNet.Dtos;
using MikadoNet.Models;

namespace MikadoNet.Services;

public class HaiinService : IHaiinService
{
    private const string ApplicationName = "Mikado-NET-Web";
    private readonly MikadoNetDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public HaiinService(MikadoNetDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<int> SaveDraftAsync(HaiinDraftRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var currentUserTantoCode = await _currentUserService.GetRequiredTantoCodeAsync(cancellationToken);

        // DraftSequence が指定された場合は同一Sequenceに上書きし、last-write-winsとする。
        var sequence = request.DraftSequence ?? await GetNextLogSequenceAsync(cancellationToken);
        var log = await _dbContext.HaiinLogs.FirstOrDefaultAsync(
            entry => entry.Sequence == sequence,
            cancellationToken);

        if (log is null)
        {
            log = new DatHaiinLog
            {
                Sequence = sequence
            };
            _dbContext.HaiinLogs.Add(log);
        }

        log.WorkDate = NormalizeWorkDate(request.WorkDate);
        log.AssigneeCode = currentUserTantoCode;
        log.WorkStartAt = request.WorkStartAt;
        log.WorkEndAt = request.WorkEndAt;
        log.WorkRequestCode = request.WorkRequestCode;
        log.WorkContent = request.WorkContent;
        log.WorkLocation = request.WorkLocation;
        log.SiteSupervisorCode = request.SiteSupervisorCode;
        log.ProcessedAt = DateTime.Now;
        log.CalendarId = request.CalendarId;
        log.ApplicationName = ApplicationName;
        log.JsonPath = request.JsonPath;
        log.EventId = request.EventId;
        log.Status = request.Operation == HaiinDraftOperation.Delete ? "Deleted" : "Journal";
        log.ErrorMessage = JsonSerializer.Serialize(new DraftMetadata
        {
            AssigneeCode = request.AssigneeCode,
            WorkGroupCode = request.WorkGroupCode,
            WorkGroupSymbol = request.WorkGroupSymbol,
            WorkGroupColorR = request.WorkGroupColorR,
            WorkGroupColorG = request.WorkGroupColorG,
            WorkGroupColorB = request.WorkGroupColorB,
            ScheduleType = request.ScheduleType,
            WorkDetails = request.WorkDetails,
            VehicleCode = request.VehicleCode
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return sequence;
    }

    public async Task<IReadOnlyList<short>> ConfirmAsync(HaiinConfirmRequest request, CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
        {
            return Array.Empty<short>();
        }

        var currentUserTantoCode = await _currentUserService.GetRequiredTantoCodeAsync(cancellationToken);
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var codes = new List<short>(request.Items.Count);
        var nextCodeByDate = new Dictionary<DateTime, short>();

        try
        {
            foreach (var item in request.Items)
            {
                if (!item.DraftLogSequence.HasValue)
                {
                    throw new InvalidOperationException("Journalが指定されていません。");
                }

                var journalLog = await _dbContext.HaiinLogs.FirstOrDefaultAsync(
                    x => x.Sequence == item.DraftLogSequence.Value
                         && x.ApplicationName == ApplicationName
                         && x.AssigneeCode == currentUserTantoCode
                         && x.Status == "Journal",
                    cancellationToken);

                if (journalLog is null)
                {
                    throw new InvalidOperationException("Journalが見つからないため確定できません。");
                }

                var workDate = NormalizeWorkDate(item.WorkDate);

                if (!nextCodeByDate.TryGetValue(workDate, out var nextCode))
                {
                    var maxCode = await GetMaxHaiinCodeAsync(workDate, cancellationToken);
                    nextCode = (short)(maxCode + 1);
                    nextCodeByDate[workDate] = nextCode;
                }

                var record = new DatHaiin
                {
                    WorkDate = workDate,
                    HaiinCode = nextCode,
                    WorkGroupCode = item.WorkGroupCode,
                    WorkGroupSymbol = item.WorkGroupSymbol,
                    WorkGroupColorR = item.WorkGroupColorR,
                    WorkGroupColorG = item.WorkGroupColorG,
                    WorkGroupColorB = item.WorkGroupColorB,
                    AssigneeCode = item.AssigneeCode,
                    ScheduleType = item.ScheduleType,
                    WorkRequestCode = item.WorkRequestCode,
                    WorkLocation = item.WorkLocation,
                    WorkContent = item.WorkContent,
                    WorkStartAt = item.WorkStartAt,
                    WorkEndAt = item.WorkEndAt,
                    WorkDetails = item.WorkDetails,
                    VehicleCode = item.VehicleCode,
                    CreatedAt = DateTime.Now,
                    CreatedBy = request.UpdatedBy,
                    UpdatedAt = DateTime.Now,
                    UpdatedBy = request.UpdatedBy,
                    SiteSupervisorCode = item.SiteSupervisorCode
                };

                _dbContext.HaiinRecords.Add(record);
                codes.Add(nextCode);

                nextCodeByDate[workDate] = (short)(nextCode + 1);

                journalLog.Status = "Confirmed";
                journalLog.HaiinCode = record.HaiinCode;
                journalLog.ProcessedAt = DateTime.Now;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await MarkJournalErrorAsync(request, currentUserTantoCode, ex, cancellationToken);
            throw;
        }

        return codes;
    }

    public async Task<IReadOnlyList<HaiinCalendarEntry>> GetCalendarAsync(HaiinCalendarRequest request, CancellationToken cancellationToken)
    {
        var startDate = NormalizeWorkDate(request.StartDate);
        var endDate = NormalizeWorkDate(request.EndDate);

        if (endDate < startDate)
        {
            return Array.Empty<HaiinCalendarEntry>();
        }

        var assigneeFilter = request.AssigneeCodes?.Where(code => code > 0).Distinct().ToList();

        var currentUserTantoCode = await _currentUserService.GetRequiredTantoCodeAsync(cancellationToken);

        var confirmedQuery = _dbContext.HaiinRecords
            .AsNoTracking()
            .Where(record => record.WorkDate >= startDate && record.WorkDate <= endDate);

        if (assigneeFilter is { Count: > 0 })
        {
            confirmedQuery = confirmedQuery.Where(record => record.AssigneeCode.HasValue && assigneeFilter.Contains(record.AssigneeCode.Value));
        }

        var confirmed = await confirmedQuery
            .Select(record => new HaiinCalendarEntry
            {
                WorkDate = record.WorkDate,
                HaiinCode = record.HaiinCode,
                AssigneeCode = record.AssigneeCode,
                WorkGroupCode = record.WorkGroupCode,
                WorkGroupSymbol = record.WorkGroupSymbol,
                WorkGroupColorR = record.WorkGroupColorR,
                WorkGroupColorG = record.WorkGroupColorG,
                WorkGroupColorB = record.WorkGroupColorB,
                ScheduleType = record.ScheduleType,
                WorkRequestCode = record.WorkRequestCode,
                WorkLocation = record.WorkLocation,
                WorkContent = record.WorkContent,
                WorkStartAt = record.WorkStartAt,
                WorkEndAt = record.WorkEndAt,
                WorkDetails = record.WorkDetails,
                VehicleCode = record.VehicleCode,
                ConfirmationMailSentAt = record.ConfirmationMailSentAt,
                CreatedAt = record.CreatedAt,
                CreatedBy = record.CreatedBy,
                SiteSupervisorCode = record.SiteSupervisorCode,
                IsDraft = false
            })
            .ToListAsync(cancellationToken);

        var draftQuery = _dbContext.HaiinLogs
            .AsNoTracking()
            .Where(log => log.Status == "Journal"
                          && log.WorkDate >= startDate
                          && log.WorkDate <= endDate
                          && log.ApplicationName == ApplicationName
                          && log.AssigneeCode == currentUserTantoCode);

        var draftLogs = await draftQuery.ToListAsync(cancellationToken);
        var drafts = draftLogs.Select(log =>
            {
                var metadata = ParseDraftMetadata(log.ErrorMessage);
                var targetAssignee = metadata?.AssigneeCode ?? log.AssigneeCode;
                return new HaiinCalendarEntry
                {
                    WorkDate = log.WorkDate ?? startDate,
                    AssigneeCode = targetAssignee,
                    WorkGroupCode = metadata?.WorkGroupCode,
                    WorkGroupSymbol = metadata?.WorkGroupSymbol,
                    WorkGroupColorR = metadata?.WorkGroupColorR,
                    WorkGroupColorG = metadata?.WorkGroupColorG,
                    WorkGroupColorB = metadata?.WorkGroupColorB,
                    ScheduleType = metadata?.ScheduleType,
                    WorkDetails = metadata?.WorkDetails,
                    VehicleCode = metadata?.VehicleCode,
                    WorkRequestCode = log.WorkRequestCode,
                    WorkContent = log.WorkContent,
                    WorkLocation = log.WorkLocation,
                    WorkStartAt = log.WorkStartAt,
                    WorkEndAt = log.WorkEndAt,
                    SiteSupervisorCode = log.SiteSupervisorCode,
                    IsDraft = true,
                    DraftSequence = log.Sequence
                };
            })
            .Where(entry => assigneeFilter is not { Count: > 0 }
                            || (entry.AssigneeCode.HasValue && assigneeFilter.Contains(entry.AssigneeCode.Value)))
            .ToList();

        return confirmed.Concat(drafts)
            .OrderBy(entry => entry.WorkDate)
            .ThenBy(entry => entry.AssigneeCode)
            .ThenBy(entry => entry.WorkStartAt)
            .ToList();
    }

    public async Task<IReadOnlyList<WorkRequestDto>> GetSagyouAsync(HaiinDataLoadRequest request, CancellationToken cancellationToken)
    {
        var sql = """
            SELECT
                DSG_作業依頼コード,
                DSG_物件コード,
                DSG_物件年度,
                DSG_サブコード,
                DSG_ビジネスシート名,
                DSG_カスタマシート名,
                DSG_仕様等,
                DSG_現地集合時間,
                DSG_工事期間開始,
                DSG_工事期間終了,
                DSG_工務区分,
                DSG_自社担当者,
                DSG_配員確定日時,
                DSG_作成日時,
                DSG_作成担当,
                DSG_更新日時,
                DSG_更新担当,
                DSG_作業内容,
                DSG_特記事項
            FROM dat_作業依頼
            ORDER BY DSG_工事期間開始 DESC, DSG_工事期間終了 DESC
            """;

        await using var command = _dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;

        if (command.Connection.State != ConnectionState.Open)
        {
            await command.Connection.OpenAsync(cancellationToken);
        }

        var results = new List<WorkRequestDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new WorkRequestDto
            {
                WorkRequestCode = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                PropertyCode = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                PropertyYear = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                SubCode = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                BusinessSheetName = reader.IsDBNull(4) ? null : reader.GetString(4),
                CustomerSheetName = reader.IsDBNull(5) ? null : reader.GetString(5),
                Specification = reader.IsDBNull(6) ? null : reader.GetString(6),
                SiteMeetTime = reader.IsDBNull(7) ? null : reader.GetString(7),
                ConstructionStart = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                ConstructionEnd = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                WorkCategory = reader.IsDBNull(10) ? null : reader.GetString(10),
                CompanyAssignee = reader.IsDBNull(11) ? null : reader.GetString(11),
                HaiinConfirmedAt = reader.IsDBNull(12) ? null : reader.GetDateTime(12),
                CreatedAt = reader.IsDBNull(13) ? null : reader.GetDateTime(13),
                CreatedBy = reader.IsDBNull(14) ? null : reader.GetInt32(14),
                UpdatedAt = reader.IsDBNull(15) ? null : reader.GetDateTime(15),
                UpdatedBy = reader.IsDBNull(16) ? null : reader.GetInt32(16),
                WorkContent = reader.IsDBNull(17) ? null : reader.GetString(17),
                Remarks = reader.IsDBNull(18) ? null : reader.GetString(18)
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<HaiinRecordDto>> GetHaiinAsync(HaiinDataLoadRequest request, CancellationToken cancellationToken)
    {
        var startDate = NormalizeWorkDate(request.StartDate);
        var endDate = NormalizeWorkDate(request.EndDate);

        var sql = """
            SELECT
                DHA_作業日,
                DHA_配員コード,
                DHA_作業グループコード,
                DHA_作業グループ記号,
                DHA_作業グループ色R,
                DHA_作業グループ色G,
                DHA_作業グループ色B,
                DHA_担当者コード,
                DHA_予定種別,
                DHA_作業依頼コード,
                DHA_作業場所,
                DHA_作業内容,
                DHA_作業開始時間,
                DHA_作業終了時間,
                DHA_作業詳細,
                DHA_車両コード,
                DHA_確認メール送信日時,
                DHA_作成日時,
                DHA_作成担当,
                DHA_更新日時,
                DHA_更新担当,
                DSG_ビジネスシート名,
                DHA_現場責任者コード
            FROM dat_配員
            LEFT OUTER JOIN dat_作業依頼 ON dat_配員.DHA_作業依頼コード = dat_作業依頼.DSG_作業依頼コード
            WHERE DHA_作業日 BETWEEN @StartDate AND @EndDate
            ORDER BY DHA_作業日, DHA_担当者コード, DHA_作業開始時間
            """;

        await using var command = _dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;

        var startParam = command.CreateParameter();
        startParam.ParameterName = "@StartDate";
        startParam.DbType = DbType.DateTime;
        startParam.Value = startDate;
        command.Parameters.Add(startParam);

        var endParam = command.CreateParameter();
        endParam.ParameterName = "@EndDate";
        endParam.DbType = DbType.DateTime;
        endParam.Value = endDate;
        command.Parameters.Add(endParam);

        if (command.Connection.State != ConnectionState.Open)
        {
            await command.Connection.OpenAsync(cancellationToken);
        }

        var results = new List<HaiinRecordDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new HaiinRecordDto
            {
                WorkDate = reader.GetDateTime(0),
                HaiinCode = reader.GetInt16(1),
                WorkGroupCode = reader.IsDBNull(2) ? null : reader.GetString(2),
                WorkGroupSymbol = reader.IsDBNull(3) ? null : reader.GetString(3),
                WorkGroupColorR = reader.IsDBNull(4) ? null : reader.GetInt16(4),
                WorkGroupColorG = reader.IsDBNull(5) ? null : reader.GetInt16(5),
                WorkGroupColorB = reader.IsDBNull(6) ? null : reader.GetInt16(6),
                AssigneeCode = reader.IsDBNull(7) ? null : reader.GetInt16(7),
                ScheduleType = reader.IsDBNull(8) ? null : reader.GetString(8),
                WorkRequestCode = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                WorkLocation = reader.IsDBNull(10) ? null : reader.GetString(10),
                WorkContent = reader.IsDBNull(11) ? null : reader.GetString(11),
                WorkStartAt = reader.IsDBNull(12) ? null : reader.GetDateTime(12),
                WorkEndAt = reader.IsDBNull(13) ? null : reader.GetDateTime(13),
                WorkDetails = reader.IsDBNull(14) ? null : reader.GetString(14),
                VehicleCode = reader.IsDBNull(15) ? null : reader.GetInt16(15),
                ConfirmationMailSentAt = reader.IsDBNull(16) ? null : reader.GetDateTime(16),
                CreatedAt = reader.IsDBNull(17) ? null : reader.GetDateTime(17),
                CreatedBy = reader.IsDBNull(18) ? null : reader.GetInt16(18),
                UpdatedAt = reader.IsDBNull(19) ? null : reader.GetDateTime(19),
                UpdatedBy = reader.IsDBNull(20) ? null : reader.GetInt16(20),
                BusinessSheetName = reader.IsDBNull(21) ? null : reader.GetString(21),
                SiteSupervisorCode = reader.IsDBNull(22) ? null : reader.GetInt16(22)
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<TantoDto>> GetTantoAsync(CancellationToken cancellationToken)
    {
        var sql = """
            SELECT
                MTA_担当者コード,
                MTA_担当者名,
                MTA_担当部署,
                MTA_管理者対象,
                MTA_表示順,
                MTA_担当者略称,
                MTA_スケジュール表示順
            FROM mst_担当者
            WHERE MTA_PM表示対象 <> 0
            ORDER BY MTA_スケジュール表示順
            """;

        await using var command = _dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;

        if (command.Connection.State != ConnectionState.Open)
        {
            await command.Connection.OpenAsync(cancellationToken);
        }

        var results = new List<TantoDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new TantoDto
            {
                AssigneeCode = reader.GetInt32(0),
                AssigneeName = reader.IsDBNull(1) ? null : reader.GetString(1),
                Department = reader.IsDBNull(2) ? null : reader.GetString(2),
                ManagerTarget = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                DisplayOrder = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                ShortName = reader.IsDBNull(5) ? null : reader.GetString(5),
                ScheduleDisplayOrder = reader.IsDBNull(6) ? null : reader.GetInt32(6)
            });
        }

        return results;
    }

    public async Task<DateTime?> GetHaiinDateAsync(CancellationToken cancellationToken)
    {
        var sql = """
            SELECT DME_配員確定日
            FROM dat_メモ
            WHERE DME_メモコード = 1
            """;

        await using var command = _dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;

        if (command.Connection.State != ConnectionState.Open)
        {
            await command.Connection.OpenAsync(cancellationToken);
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result == DBNull.Value ? null : Convert.ToDateTime(result);
    }

    public async Task<DateTime?> GetLastEditHaiinAsync(CancellationToken cancellationToken)
    {
        var sql = "SELECT MAX(DHA_更新日時) FROM dat_配員";

        await using var command = _dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;

        if (command.Connection.State != ConnectionState.Open)
        {
            await command.Connection.OpenAsync(cancellationToken);
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result == DBNull.Value ? null : Convert.ToDateTime(result);
    }

    public async Task<DateTime?> GetLastSendMailAsync(CancellationToken cancellationToken)
    {
        var sql = "SELECT MAX(DHA_確認メール送信日時) FROM dat_配員";

        await using var command = _dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;

        if (command.Connection.State != ConnectionState.Open)
        {
            await command.Connection.OpenAsync(cancellationToken);
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result == DBNull.Value ? null : Convert.ToDateTime(result);
    }

    public async Task UpdateHaiinDateAsync(DateTime haiinDate, CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await using var command = _dbContext.Database.GetDbConnection().CreateCommand();
        command.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = """
            UPDATE dat_メモ
            SET DME_配員確定日 = @HaiinDate
            WHERE DME_メモコード = 1
            """;

        var dateParam = command.CreateParameter();
        dateParam.ParameterName = "@HaiinDate";
        dateParam.DbType = DbType.DateTime;
        dateParam.Value = haiinDate;
        command.Parameters.Add(dateParam);

        if (command.Connection.State != ConnectionState.Open)
        {
            await command.Connection.OpenAsync(cancellationToken);
        }

        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<HaiinMailBatch> GetMailBatchAsync(DateTime? since, CancellationToken cancellationToken)
    {
        var sinceDate = since ?? await GetLastSendMailAsync(cancellationToken) ?? DateTime.MinValue;

        var sql = """
            SELECT
                DHA_担当者コード,
                DHA_作業依頼コード,
                DHA_作業開始時間,
                DHA_作業終了時間,
                DHA_作業場所,
                DHA_作業内容,
                DHA_作業詳細,
                MSR_車両名,
                DSG_ビジネスシート名,
                DSG_カスタマシート名,
                MTA_携帯メールアドレス
            FROM dat_配員
            LEFT OUTER JOIN mst_担当者 ON dat_配員.DHA_担当者コード = mst_担当者.MTA_担当者コード
            LEFT OUTER JOIN dat_作業依頼 ON dat_配員.DHA_作業依頼コード = dat_作業依頼.DSG_作業依頼コード
            LEFT OUTER JOIN mst_車両 ON dat_配員.DHA_車両コード = mst_車両.MSR_車両コード
            WHERE DHA_作成日時 > @SinceDate
              AND DHA_作業依頼コード IS NOT NULL
              AND MTA_PMメール対象 <> 0
            ORDER BY DHA_担当者コード, DHA_作業依頼コード, DHA_作業日
            """;

        await using var command = _dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;

        var sinceParam = command.CreateParameter();
        sinceParam.ParameterName = "@SinceDate";
        sinceParam.DbType = DbType.DateTime;
        sinceParam.Value = sinceDate;
        command.Parameters.Add(sinceParam);

        if (command.Connection.State != ConnectionState.Open)
        {
            await command.Connection.OpenAsync(cancellationToken);
        }

        var groups = new Dictionary<(short AssigneeCode, int WorkRequestCode), HaiinMailGroup>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var assigneeCode = reader.IsDBNull(0) ? (short)0 : reader.GetInt16(0);
            var workRequestCode = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            var key = (assigneeCode, workRequestCode);

            if (!groups.TryGetValue(key, out var group))
            {
                group = new HaiinMailGroup
                {
                    AssigneeCode = assigneeCode,
                    WorkRequestCode = workRequestCode,
                    BusinessSheetName = reader.IsDBNull(8) ? null : reader.GetString(8),
                    CustomerSheetName = reader.IsDBNull(9) ? null : reader.GetString(9),
                    AssigneeEmail = reader.IsDBNull(10) ? null : reader.GetString(10),
                    Entries = new List<HaiinMailEntry>()
                };
                groups[key] = group;
            }

            ((List<HaiinMailEntry>)group.Entries).Add(new HaiinMailEntry
            {
                WorkStartAt = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                WorkEndAt = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                WorkLocation = reader.IsDBNull(4) ? null : reader.GetString(4),
                WorkContent = reader.IsDBNull(5) ? null : reader.GetString(5),
                WorkDetails = reader.IsDBNull(6) ? null : reader.GetString(6),
                VehicleName = reader.IsDBNull(7) ? null : reader.GetString(7)
            });
        }

        return new HaiinMailBatch
        {
            Since = sinceDate,
            Groups = groups.Values.ToList()
        };
    }

    public async Task MarkMailSentAsync(DateTime sentAt, DateTime? since, CancellationToken cancellationToken)
    {
        var sinceDate = since ?? await GetLastSendMailAsync(cancellationToken) ?? DateTime.MinValue;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await using var command = _dbContext.Database.GetDbConnection().CreateCommand();
        command.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = """
            UPDATE dat_配員
            SET DHA_確認メール送信日時 = @SentAt
            WHERE DHA_作成日時 > @SinceDate
              AND DHA_作業依頼コード IS NOT NULL
            """;

        var sentParam = command.CreateParameter();
        sentParam.ParameterName = "@SentAt";
        sentParam.DbType = DbType.DateTime;
        sentParam.Value = sentAt;
        command.Parameters.Add(sentParam);

        var sinceParam = command.CreateParameter();
        sinceParam.ParameterName = "@SinceDate";
        sinceParam.DbType = DbType.DateTime;
        sinceParam.Value = sinceDate;
        command.Parameters.Add(sinceParam);

        if (command.Connection.State != ConnectionState.Open)
        {
            await command.Connection.OpenAsync(cancellationToken);
        }

        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateSagyouConfirmedAtAsync(int workRequestCode, DateTime confirmedAt, CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await using var command = _dbContext.Database.GetDbConnection().CreateCommand();
        command.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = """
            UPDATE dat_作業依頼
            SET DSG_配員確定日時 = @ConfirmedAt
            WHERE DSG_作業依頼コード = @WorkRequestCode
            """;

        var dateParam = command.CreateParameter();
        dateParam.ParameterName = "@ConfirmedAt";
        dateParam.DbType = DbType.DateTime;
        dateParam.Value = confirmedAt;
        command.Parameters.Add(dateParam);

        var codeParam = command.CreateParameter();
        codeParam.ParameterName = "@WorkRequestCode";
        codeParam.DbType = DbType.Int32;
        codeParam.Value = workRequestCode;
        command.Parameters.Add(codeParam);

        if (command.Connection.State != ConnectionState.Open)
        {
            await command.Connection.OpenAsync(cancellationToken);
        }

        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<HaiinNotificationInfo?> GetNotificationInfoAsync(HaiinNotificationRequest request, CancellationToken cancellationToken)
    {
        var scheduleSql = """
            SELECT dat_配員.DHA_作業日, mst_担当者.MTA_担当者名
            FROM dat_配員
            LEFT OUTER JOIN dat_作業依頼 ON dat_配員.DHA_作業依頼コード = dat_作業依頼.DSG_作業依頼コード
            LEFT OUTER JOIN mst_担当者 ON dat_配員.DHA_担当者コード = mst_担当者.MTA_担当者コード
            WHERE dat_配員.DHA_作業依頼コード = @WorkRequestCode
            ORDER BY dat_配員.DHA_作業日
            """;

        await using var scheduleCommand = _dbContext.Database.GetDbConnection().CreateCommand();
        scheduleCommand.CommandText = scheduleSql;

        var workParam = scheduleCommand.CreateParameter();
        workParam.ParameterName = "@WorkRequestCode";
        workParam.DbType = DbType.Int32;
        workParam.Value = request.WorkRequestCode;
        scheduleCommand.Parameters.Add(workParam);

        if (scheduleCommand.Connection.State != ConnectionState.Open)
        {
            await scheduleCommand.Connection.OpenAsync(cancellationToken);
        }

        var scheduleMap = new Dictionary<DateTime, List<string>>();
        await using (var reader = await scheduleCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                if (reader.IsDBNull(0))
                {
                    continue;
                }

                var workDate = reader.GetDateTime(0).Date;
                var name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);

                if (!scheduleMap.TryGetValue(workDate, out var names))
                {
                    names = new List<string>();
                    scheduleMap[workDate] = names;
                }

                if (!string.IsNullOrWhiteSpace(name))
                {
                    names.Add(name);
                }
            }
        }

        var detailSql = """
            SELECT DISTINCT
                dat_物件.DBU_年度,
                dat_物件.DBU_物件コード,
                dat_物件.DBU_サブコード,
                dat_物件.DBU_物件名,
                dat_物件.DBU_物件名2,
                dat_物件.DBU_物件名3,
                mst_得意先.MTO_得意先名,
                mst_担当者.MTA_担当者名,
                MTA_PCメールアドレス,
                dat_作業依頼.DSG_作業依頼コード
            FROM mst_担当者
            RIGHT OUTER JOIN dat_物件 ON mst_担当者.MTA_担当者コード = dat_物件.DBU_担当者コード
            RIGHT OUTER JOIN dat_作業依頼
                INNER JOIN dat_工事 ON dat_作業依頼.DSG_作業依頼コード = dat_工事.DKO_作業依頼コード
                ON dat_物件.DBU_物件コード = dat_工事.DKO_物件コード
                AND dat_物件.DBU_サブコード = dat_工事.DKO_サブコード
                AND dat_物件.DBU_年度 = dat_工事.DKO_年度
            FULL OUTER JOIN mst_得意先 ON dat_物件.DBU_得意先コード = mst_得意先.MTO_得意先コード
            WHERE dat_作業依頼.DSG_作業依頼コード = @WorkRequestCode
            """;

        await using var detailCommand = _dbContext.Database.GetDbConnection().CreateCommand();
        detailCommand.CommandText = detailSql;

        var detailParam = detailCommand.CreateParameter();
        detailParam.ParameterName = "@WorkRequestCode";
        detailParam.DbType = DbType.Int32;
        detailParam.Value = request.WorkRequestCode;
        detailCommand.Parameters.Add(detailParam);

        if (detailCommand.Connection.State != ConnectionState.Open)
        {
            await detailCommand.Connection.OpenAsync(cancellationToken);
        }

        await using var detailReader = await detailCommand.ExecuteReaderAsync(cancellationToken);
        if (!await detailReader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var propertyCode = $"{detailReader.GetValue(0)}-{detailReader.GetValue(1)}-{detailReader.GetValue(2)}";
        var propertyName = $"{detailReader.GetValue(3)}_{detailReader.GetValue(4)}_{detailReader.GetValue(5)}";

        return new HaiinNotificationInfo
        {
            PropertyCode = propertyCode,
            PropertyName = propertyName,
            CustomerName = detailReader.IsDBNull(6) ? null : detailReader.GetString(6),
            SalesAssigneeName = detailReader.IsDBNull(7) ? null : detailReader.GetString(7),
            SalesAssigneeEmail = detailReader.IsDBNull(8) ? null : detailReader.GetString(8),
            WorkRequestCode = detailReader.IsDBNull(9) ? 0 : detailReader.GetInt32(9),
            Schedules = scheduleMap
                .OrderBy(pair => pair.Key)
                .Select(pair => new HaiinNotificationSchedule
                {
                    WorkDate = pair.Key,
                    AssigneeNames = pair.Value
                })
                .ToList()
        };
    }

    public async Task<WorkRequestSummary?> GetWorkRequestAsync(int workRequestCode, CancellationToken cancellationToken)
    {
        var sql = """
            SELECT
                DSG_作業依頼コード,
                DSG_ビジネスシート名,
                DSG_カスタマシート名,
                DSG_自社担当者
            FROM dat_作業依頼
            WHERE DSG_作業依頼コード = @WorkRequestCode
            """;

        await using var command = _dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;

        var codeParam = command.CreateParameter();
        codeParam.ParameterName = "@WorkRequestCode";
        codeParam.DbType = DbType.Int32;
        codeParam.Value = workRequestCode;
        command.Parameters.Add(codeParam);

        if (command.Connection.State != ConnectionState.Open)
        {
            await command.Connection.OpenAsync(cancellationToken);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new WorkRequestSummary
        {
            WorkRequestCode = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
            BusinessSheetName = reader.IsDBNull(1) ? null : reader.GetString(1),
            CustomerSheetName = reader.IsDBNull(2) ? null : reader.GetString(2),
            CompanyAssignee = reader.IsDBNull(3) ? null : reader.GetString(3)
        };
    }

    public async Task<SupervisorInfo?> GetSupervisorInfoAsync(int assigneeCode, CancellationToken cancellationToken)
    {
        var sql = """
            SELECT MTA_担当者コード, MTA_担当者名, MTA_PCメールアドレス
            FROM mst_担当者
            WHERE MTA_担当部署 NOT LIKE '%工事%'
              AND MTA_担当者コード = @AssigneeCode
            """;

        await using var command = _dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;

        var codeParam = command.CreateParameter();
        codeParam.ParameterName = "@AssigneeCode";
        codeParam.DbType = DbType.Int32;
        codeParam.Value = assigneeCode;
        command.Parameters.Add(codeParam);

        if (command.Connection.State != ConnectionState.Open)
        {
            await command.Connection.OpenAsync(cancellationToken);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new SupervisorInfo
        {
            AssigneeCode = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
            AssigneeName = reader.IsDBNull(1) ? null : reader.GetString(1),
            Email = reader.IsDBNull(2) ? null : reader.GetString(2)
        };
    }

    private async Task<short> GetMaxHaiinCodeAsync(DateTime workDate, CancellationToken cancellationToken)
    {
        await using var command = _dbContext.Database.GetDbConnection().CreateCommand();
        command.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = """
            SELECT ISNULL(MAX([DHA_配員コード]), 0)
            FROM [dbo].[dat_配員] WITH (UPDLOCK, HOLDLOCK)
            WHERE [DHA_作業日] = @WorkDate
            """;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@WorkDate";
        parameter.DbType = DbType.DateTime;
        parameter.Value = workDate;
        command.Parameters.Add(parameter);

        if (command.Connection.State != ConnectionState.Open)
        {
            await command.Connection.OpenAsync(cancellationToken);
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt16(result);
    }

    private async Task<int> GetNextLogSequenceAsync(CancellationToken cancellationToken)
    {
        await using var command = _dbContext.Database.GetDbConnection().CreateCommand();
        command.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = """
            SELECT ISNULL(MAX([DHL_連番]), 0) + 1
            FROM [dbo].[dat_配員ログ] WITH (UPDLOCK, HOLDLOCK)
            """;

        if (command.Connection.State != ConnectionState.Open)
        {
            await command.Connection.OpenAsync(cancellationToken);
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    private static DateTime NormalizeWorkDate(DateTime value)
    {
        return value.Date;
    }

    private static DraftMetadata? ParseDraftMetadata(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<DraftMetadata>(payload);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task MarkJournalErrorAsync(
        HaiinConfirmRequest request,
        short currentUserTantoCode,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var sequences = request.Items
            .Where(item => item.DraftLogSequence.HasValue)
            .Select(item => item.DraftLogSequence!.Value)
            .Distinct()
            .ToList();

        if (sequences.Count == 0)
        {
            return;
        }

        await using var errorTransaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var logs = await _dbContext.HaiinLogs
            .Where(log => sequences.Contains(log.Sequence)
                          && log.ApplicationName == ApplicationName
                          && log.AssigneeCode == currentUserTantoCode)
            .ToListAsync(cancellationToken);

        foreach (var log in logs)
        {
            log.Status = "Error";
            log.ErrorMessage = exception.ToString();
            log.ProcessedAt = DateTime.Now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await errorTransaction.CommitAsync(cancellationToken);
    }

    private class DraftMetadata
    {
        public short? AssigneeCode { get; set; }
        public string? WorkGroupCode { get; set; }
        public string? WorkGroupSymbol { get; set; }
        public short? WorkGroupColorR { get; set; }
        public short? WorkGroupColorG { get; set; }
        public short? WorkGroupColorB { get; set; }
        public string? ScheduleType { get; set; }
        public string? WorkDetails { get; set; }
        public short? VehicleCode { get; set; }
    }
}
