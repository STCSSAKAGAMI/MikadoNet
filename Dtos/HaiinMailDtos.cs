namespace MikadoNet.Dtos;

public class HaiinMailBatch
{
    public DateTime Since { get; set; }
    public IReadOnlyList<HaiinMailGroup> Groups { get; set; } = Array.Empty<HaiinMailGroup>();
}

public class HaiinMailGroup
{
    public short AssigneeCode { get; set; }
    public int WorkRequestCode { get; set; }
    public string? AssigneeEmail { get; set; }
    public string? BusinessSheetName { get; set; }
    public string? CustomerSheetName { get; set; }
    public IReadOnlyList<HaiinMailEntry> Entries { get; set; } = Array.Empty<HaiinMailEntry>();
}

public class HaiinMailEntry
{
    public DateTime? WorkStartAt { get; set; }
    public DateTime? WorkEndAt { get; set; }
    public string? WorkLocation { get; set; }
    public string? WorkContent { get; set; }
    public string? WorkDetails { get; set; }
    public string? VehicleName { get; set; }
}

public class HaiinMailSentRequest
{
    public DateTime SentAt { get; set; }
    public DateTime? Since { get; set; }
}

public class HaiinSagyouConfirmedRequest
{
    public int WorkRequestCode { get; set; }
    public DateTime ConfirmedAt { get; set; }
}

public class HaiinNotificationRequest
{
    public int WorkRequestCode { get; set; }
}

public class HaiinNotificationInfo
{
    public string? PropertyCode { get; set; }
    public string? PropertyName { get; set; }
    public string? CustomerName { get; set; }
    public string? SalesAssigneeName { get; set; }
    public string? SalesAssigneeEmail { get; set; }
    public int WorkRequestCode { get; set; }
    public IReadOnlyList<HaiinNotificationSchedule> Schedules { get; set; } = Array.Empty<HaiinNotificationSchedule>();
}

public class HaiinNotificationSchedule
{
    public DateTime WorkDate { get; set; }
    public IReadOnlyList<string> AssigneeNames { get; set; } = Array.Empty<string>();
}
