using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartHomeApplicationAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPresetsAndDevicePresetForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_devices_presets_preset_id",
                table: "devices");

            migrationBuilder.DropIndex(
                name: "IX_devices_preset_id",
                table: "devices");

            migrationBuilder.AddColumn<int>(
                name: "device_id",
                table: "presets",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                WITH ranked_devices AS (
                    SELECT
                        d."id" AS device_id,
                        d."preset_id",
                        ROW_NUMBER() OVER (PARTITION BY d."preset_id" ORDER BY d."id") AS row_number
                    FROM "devices" AS d
                )
                UPDATE "presets" AS p
                SET "device_id" = r."device_id"
                FROM ranked_devices AS r
                WHERE r."preset_id" = p."id"
                  AND r.row_number = 1;
                """);

            migrationBuilder.Sql("""
                INSERT INTO "presets"
                    ("name", "consumption_in_kwh", "duration_in_minutes", "device_id")
                SELECT
                    p."name",
                    p."consumption_in_kwh",
                    p."duration_in_minutes",
                    r."device_id"
                FROM "presets" AS p
                INNER JOIN (
                    SELECT
                        d."id" AS device_id,
                        d."preset_id",
                        ROW_NUMBER() OVER (PARTITION BY d."preset_id" ORDER BY d."id") AS row_number
                    FROM "devices" AS d
                ) AS r ON r."preset_id" = p."id"
                WHERE r.row_number > 1;
                """);

            migrationBuilder.Sql("""
                DELETE FROM "presets"
                WHERE "device_id" IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "device_id",
                table: "presets",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "preset_id",
                table: "devices");

            migrationBuilder.CreateIndex(
                name: "IX_presets_device_id",
                table: "presets",
                column: "device_id");

            migrationBuilder.AddForeignKey(
                name: "FK_presets_devices_device_id",
                table: "presets",
                column: "device_id",
                principalTable: "devices",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_presets_devices_device_id",
                table: "presets");

            migrationBuilder.DropIndex(
                name: "IX_presets_device_id",
                table: "presets");

            migrationBuilder.AddColumn<int>(
                name: "preset_id",
                table: "devices",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "devices" AS d
                SET "preset_id" = p."id"
                FROM "presets" AS p
                WHERE p."device_id" = d."id";
                """);

            migrationBuilder.DropColumn(
                name: "device_id",
                table: "presets");

            migrationBuilder.AlterColumn<int>(
                name: "preset_id",
                table: "devices",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_devices_preset_id",
                table: "devices",
                column: "preset_id");

            migrationBuilder.AddForeignKey(
                name: "FK_devices_presets_preset_id",
                table: "devices",
                column: "preset_id",
                principalTable: "presets",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
