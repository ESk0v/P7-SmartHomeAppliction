using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartHomeApplicationAPI.Migrations
{
    /// <inheritdoc />
    public partial class updatedDeviceConsumption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "preset_name",
                table: "device_preset",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<decimal>(
                name: "consumption_in_kwh",
                table: "device_preset",
                type: "numeric(7,3)",
                precision: 7,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,3)",
                oldPrecision: 5,
                oldScale: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "preset_name",
                table: "device_preset",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "consumption_in_kwh",
                table: "device_preset",
                type: "numeric(5,3)",
                precision: 5,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(7,3)",
                oldPrecision: 7,
                oldScale: 3);
        }
    }
}
