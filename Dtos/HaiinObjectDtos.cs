namespace MikadoNet.Dtos;

public class WorkRequestSummary
{
    public int WorkRequestCode { get; set; }
    public string? BusinessSheetName { get; set; }
    public string? CustomerSheetName { get; set; }
    public string? CompanyAssignee { get; set; }
}

public class SupervisorInfo
{
    public int AssigneeCode { get; set; }
    public string? AssigneeName { get; set; }
    public string? Email { get; set; }
}
