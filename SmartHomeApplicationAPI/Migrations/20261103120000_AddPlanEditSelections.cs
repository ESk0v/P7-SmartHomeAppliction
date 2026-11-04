using Microsoft.EntityFrameworkCore.Migrations;

namespace SmartHomeApplicationAPI.Migrations;

public partial class AddPlanEditSelections : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "device_preset_id",
            table: "plan",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "charge_stop_percentage",
            table: "plan",
            type: "integer",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "device_preset_id", table: "plan");
        migrationBuilder.DropColumn(name: "charge_stop_percentage", table: "plan");
    }
}
