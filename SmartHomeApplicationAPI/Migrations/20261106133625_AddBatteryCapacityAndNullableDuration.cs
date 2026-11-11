using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartHomeApplicationAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddBatteryCapacityAndNullableDuration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "duration_in_minutes",
                table: "device_preset",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<decimal>(
                name: "battery_capacity_in_kwh",
                table: "device_preset",
                type: "numeric(7,3)",
                precision: 7,
                scale: 3,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "battery_capacity_in_kwh",
                table: "device_preset");

            migrationBuilder.AlterColumn<int>(
                name: "duration_in_minutes",
                table: "device_preset",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
