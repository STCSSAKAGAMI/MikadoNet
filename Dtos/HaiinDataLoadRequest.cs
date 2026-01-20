namespace MikadoNet.Dtos;

public class HaiinDataLoadRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class WorkRequestDto
{
    public int WorkRequestCode { get; set; }
    public int PropertyCode { get; set; }
    public int PropertyYear { get; set; }
    public int SubCode { get; set; }
    public string? BusinessSheetName { get; set; }
    public string? CustomerSheetName { get; set; }
    public string? Specification { get; set; }
    public string? SiteMeetTime { get; set; }
    public DateTime? ConstructionStart { get; set; }
    public DateTime? ConstructionEnd { get; set; }
    public string? WorkCategory { get; set; }
    public string? CompanyAssignee { get; set; }
    public string? WorkContent { get; set; }
    public string? Remarks { get; set; }
    public DateTime? HaiinConfirmedAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}

public class HaiinRecordDto
{
    public DateTime WorkDate { get; set; }
    public short HaiinCode { get; set; }
    public string? WorkGroupCode { get; set; }
    public string? WorkGroupSymbol { get; set; }
    public short? WorkGroupColorR { get; set; }
    public short? WorkGroupColorG { get; set; }
    public short? WorkGroupColorB { get; set; }
    public short? AssigneeCode { get; set; }
    public string? ScheduleType { get; set; }
    public int? WorkRequestCode { get; set; }
    public string? WorkLocation { get; set; }
    public string? WorkContent { get; set; }
    public DateTime? WorkStartAt { get; set; }
    public DateTime? WorkEndAt { get; set; }
    public string? WorkDetails { get; set; }
    public short? VehicleCode { get; set; }
    public DateTime? ConfirmationMailSentAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public short? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public short? UpdatedBy { get; set; }
    public string? BusinessSheetName { get; set; }
    public short? SiteSupervisorCode { get; set; }
    public string Status { get; set; } = "Confirmed";
}

public class TantoDto
{
    public int AssigneeCode { get; set; }
    public string? AssigneeName { get; set; }
    public string? Department { get; set; }
    public int? ManagerTarget { get; set; }
    public int? DisplayOrder { get; set; }
    public string? ShortName { get; set; }
    public int? ScheduleDisplayOrder { get; set; }
}
