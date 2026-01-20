namespace MikadoNet.Dtos;

public class HaiinDraftRequest
{
    public DateTime WorkDate { get; set; }
    public int? DraftSequence { get; set; }
    public HaiinDraftOperation Operation { get; set; } = HaiinDraftOperation.Save;
    public short? AssigneeCode { get; set; }
    public string? WorkGroupCode { get; set; }
    public string? WorkGroupSymbol { get; set; }
    public short? WorkGroupColorR { get; set; }
    public short? WorkGroupColorG { get; set; }
    public short? WorkGroupColorB { get; set; }
    public string? ScheduleType { get; set; }
    public DateTime? WorkStartAt { get; set; }
    public DateTime? WorkEndAt { get; set; }
    public int? WorkRequestCode { get; set; }
    public string? WorkContent { get; set; }
    public string? WorkLocation { get; set; }
    public string? WorkDetails { get; set; }
    public short? VehicleCode { get; set; }
    public short? SiteSupervisorCode { get; set; }
    public string? CalendarId { get; set; }
    public string? ApplicationName { get; set; }
    public string? EventId { get; set; }
    public string? JsonPath { get; set; }
}

public enum HaiinDraftOperation
{
    Save = 0,
    Delete = 1
}
