namespace MikadoNet.Dtos;

public class HaiinConfirmRequest
{
    public short? UpdatedBy { get; set; }
    public List<HaiinConfirmItem> Items { get; set; } = new();
}

public class HaiinConfirmItem
{
    public DateTime WorkDate { get; set; }
    public short? AssigneeCode { get; set; }
    public string? WorkGroupCode { get; set; }
    public string? WorkGroupSymbol { get; set; }
    public short? WorkGroupColorR { get; set; }
    public short? WorkGroupColorG { get; set; }
    public short? WorkGroupColorB { get; set; }
    public string? ScheduleType { get; set; }
    public int? WorkRequestCode { get; set; }
    public string? WorkLocation { get; set; }
    public string? WorkContent { get; set; }
    public DateTime? WorkStartAt { get; set; }
    public DateTime? WorkEndAt { get; set; }
    public string? WorkDetails { get; set; }
    public short? VehicleCode { get; set; }
    public short? SiteSupervisorCode { get; set; }
    public int? DraftLogSequence { get; set; }
}
