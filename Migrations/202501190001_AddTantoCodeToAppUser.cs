using Microsoft.EntityFrameworkCore.Migrations;

namespace MikadoNet.Migrations;

public partial class AddTantoCodeToAppUser : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<short>(
            name: "TantoCode",
            table: "AspNetUsers",
            type: "smallint",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "TantoCode",
            table: "AspNetUsers");
    }
}
