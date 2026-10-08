using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
                name: "FK_schedules_device_device_id",
                table: "schedules");

            migrationBuilder.CreateTable(
                name: "presets",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    consumption_in_kwh = table.Column<decimal>(type: "numeric", nullable: false),
                    duration_in_minutes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_presets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "devices",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    icon = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    preset_id = table.Column<int>(type: "integer", nullable: false),
                    consumption_in_kwh = table.Column<decimal>(type: "numeric", nullable: false),
                    duration_in_minutes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_devices", x => x.id);
                    table.ForeignKey(
                        name: "FK_devices_presets_preset_id",
                        column: x => x.preset_id,
                        principalTable: "presets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

                migrationBuilder.Sql("""
                    INSERT INTO "presets" ("name", "consumption_in_kwh", "duration_in_minutes")
                    SELECT DISTINCT ON (d."preset")
                        d."preset",
                        d."consumption_in_kwh",
                        d."duration_in_minutes"
                    FROM "device" AS d
                    ORDER BY d."preset", d."id";
                    """);

                migrationBuilder.Sql("""
                    INSERT INTO "devices"
                        ("id", "name", "category", "icon", "preset_id",
                         "consumption_in_kwh", "duration_in_minutes")
                    SELECT
                        d."id",
                        d."name",
                        d."category",
                        d."icon",
                        p."id",
                        d."consumption_in_kwh",
                        d."duration_in_minutes"
                    FROM "device" AS d
                    INNER JOIN "presets" AS p ON p."name" = d."preset";
                    """);

                migrationBuilder.Sql("""
                    SELECT setval(
                        pg_get_serial_sequence('"devices"', 'id'),
                        COALESCE(MAX("id"), 1),
                        COUNT(*) > 0
                    )
                    FROM "devices";
                    """);

                migrationBuilder.DropTable(
                    name: "device");

                migrationBuilder.CreateIndex(
                    name: "IX_devices_preset_id",
                table: "devices",
                column: "preset_id");

            migrationBuilder.AddForeignKey(
                name: "FK_schedules_devices_device_id",
                table: "schedules",
                column: "device_id",
                principalTable: "devices",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_schedules_devices_device_id",
                table: "schedules");

            migrationBuilder.DropTable(
                name: "devices");

            migrationBuilder.DropTable(
                name: "presets");

            migrationBuilder.CreateTable(
                name: "device",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    consumption_in_kwh = table.Column<decimal>(type: "numeric", nullable: false),
                    duration_in_minutes = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    preset = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device", x => x.id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_schedules_device_device_id",
                table: "schedules",
                column: "device_id",
                principalTable: "device",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
