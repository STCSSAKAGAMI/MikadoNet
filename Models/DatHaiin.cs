using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MikadoNet.Models;

[Table("dat_配員")]
public class DatHaiin
{
    [Key, Column("DHA_作業日", TypeName = "datetime")]
    public DateTime WorkDate { get; set; }

    [Key, Column("DHA_配員コード")]
    public short HaiinCode { get; set; }

    [Column("DHA_作業グループコード")]
    public string? WorkGroupCode { get; set; }

    [Column("DHA_作業グループ記号")]
    public string? WorkGroupSymbol { get; set; }

    [Column("DHA_作業グループ色R")]
    public short? WorkGroupColorR { get; set; }

    [Column("DHA_作業グループ色G")]
    public short? WorkGroupColorG { get; set; }

    [Column("DHA_作業グループ色B")]
    public short? WorkGroupColorB { get; set; }

    [Column("DHA_担当者コード")]
    public short? AssigneeCode { get; set; }

    [Column("DHA_予定種別")]
    public string? ScheduleType { get; set; }

    [Column("DHA_作業依頼コード")]
    public int? WorkRequestCode { get; set; }

    [Column("DHA_作業場所")]
    public string? WorkLocation { get; set; }

    [Column("DHA_作業内容")]
    public string? WorkContent { get; set; }

    [Column("DHA_作業開始時間", TypeName = "datetime")]
    public DateTime? WorkStartAt { get; set; }

    [Column("DHA_作業終了時間", TypeName = "datetime")]
    public DateTime? WorkEndAt { get; set; }

    [Column("DHA_作業詳細")]
    public string? WorkDetails { get; set; }

    [Column("DHA_車両コード")]
    public short? VehicleCode { get; set; }

    [Column("DHA_確認メール送信日時", TypeName = "datetime")]
    public DateTime? ConfirmationMailSentAt { get; set; }

    [Column("DHA_作成日時", TypeName = "datetime")]
    public DateTime? CreatedAt { get; set; }

    [Column("DHA_作成担当")]
    public short? CreatedBy { get; set; }

    [Column("DHA_更新日時", TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }

    [Column("DHA_更新担当")]
    public short? UpdatedBy { get; set; }

    [Column("DHA_現場責任者コード")]
    public short? SiteSupervisorCode { get; set; }
}
