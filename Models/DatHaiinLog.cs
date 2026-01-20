using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MikadoNet.Models;

[Table("dat_配員ログ")]
public class DatHaiinLog
{
    [Key, Column("DHL_連番")]
    public int Sequence { get; set; }

    [Column("DHL_作業日", TypeName = "datetime")]
    public DateTime? WorkDate { get; set; }

    [Column("DHL_配員コード")]
    public short? HaiinCode { get; set; }

    [Column("DHL_担当者コード")]
    public short? AssigneeCode { get; set; }

    [Column("DHL_作業開始時間", TypeName = "datetime")]
    public DateTime? WorkStartAt { get; set; }

    [Column("DHL_作業終了時間", TypeName = "datetime")]
    public DateTime? WorkEndAt { get; set; }

    [Column("DHL_作業依頼コード")]
    public int? WorkRequestCode { get; set; }

    [Column("DHL_作業内容")]
    public string? WorkContent { get; set; }

    [Column("DHL_作業場所")]
    public string? WorkLocation { get; set; }

    [Column("DHL_現場責任者コード")]
    public short? SiteSupervisorCode { get; set; }

    [Column("DHL_処理日時", TypeName = "datetime")]
    public DateTime? ProcessedAt { get; set; }

    [Column("DHL_CalendarID")]
    public string? CalendarId { get; set; }

    [Column("DHL_ApplicationName")]
    public string? ApplicationName { get; set; }

    [Column("DHL_JSONパス")]
    public string? JsonPath { get; set; }

    [Column("DHL_EventID")]
    public string? EventId { get; set; }

    [Column("DHL_状況")]
    public string? Status { get; set; }

    [Column("DHL_エラー内容")]
    public string? ErrorMessage { get; set; }
}
