using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SmartHomeApplicationAPI.Migrations
{
    /// <inheritdoc />
    public partial class DatabaseRestructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "presets");

            migrationBuilder.DropTable(
                name: "schedules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_devices",
                table: "devices");

            migrationBuilder.DropColumn(
                name: "consumption_in_kwh",
                table: "devices");

            migrationBuilder.DropColumn(
                name: "duration_in_minutes",
                table: "devices");

            migrationBuilder.DropColumn(
                name: "icon",
                table: "devices");

            migrationBuilder.DropColumn(
                name: "name",
                table: "devices");

            migrationBuilder.RenameTable(
                name: "devices",
                newName: "device");

            migrationBuilder.AddColumn<string>(
                name: "model",
                table: "device",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_device",
                table: "device",
                column: "id");

            migrationBuilder.CreateTable(
                name: "device_preset",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    device_id = table.Column<int>(type: "integer", nullable: false),
                    preset_name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    consumption_in_kwh = table.Column<decimal>(type: "numeric(5,3)", precision: 5, scale: 3, nullable: false),
                    duration_in_minutes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_preset", x => x.id);
                    table.ForeignKey(
                        name: "FK_device_preset_device_device_id",
                        column: x => x.device_id,
                        principalTable: "device",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_device",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    device_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    nickname = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    icon = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    is_favorite = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_device", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_device_AspNetUsers_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_device_device_device_id",
                        column: x => x.device_id,
                        principalTable: "device",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "plan",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_device_id = table.Column<int>(type: "integer", nullable: false),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    next_plan_id = table.Column<int>(type: "integer", nullable: true),
                    estimated_price_in_kroner = table.Column<decimal>(type: "money", nullable: false),
                    estimated_co2_emissions_in_percent = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan", x => x.id);
                    table.ForeignKey(
                        name: "FK_plan_plan_next_plan_id",
                        column: x => x.next_plan_id,
                        principalTable: "plan",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_plan_user_device_user_device_id",
                        column: x => x.user_device_id,
                        principalTable: "user_device",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_device_preset_device_id",
                table: "device_preset",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "IX_plan_next_plan_id",
                table: "plan",
                column: "next_plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_plan_user_device_id",
                table: "plan",
                column: "user_device_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_device_device_id",
                table: "user_device",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_device_user_id",
                table: "user_device",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "device_preset");

            migrationBuilder.DropTable(
                name: "plan");

            migrationBuilder.DropTable(
                name: "user_device");

            migrationBuilder.DropPrimaryKey(
                name: "PK_device",
                table: "device");

            migrationBuilder.DropColumn(
                name: "model",
                table: "device");

            migrationBuilder.RenameTable(
                name: "device",
                newName: "devices");

            migrationBuilder.AddColumn<decimal>(
                name: "consumption_in_kwh",
                table: "devices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "duration_in_minutes",
                table: "devices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "icon",
                table: "devices",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "devices",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_devices",
                table: "devices",
                column: "id");

            migrationBuilder.CreateTable(
                name: "presets",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    device_id = table.Column<int>(type: "integer", nullable: false),
                    consumption_in_kwh = table.Column<decimal>(type: "numeric", nullable: false),
                    duration_in_minutes = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_presets", x => x.id);
                    table.ForeignKey(
                        name: "FK_presets_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "schedules",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    device_id = table.Column<int>(type: "integer", nullable: false),
                    next_schedule_id = table.Column<int>(type: "integer", nullable: true),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    estimated_co2_emissions_in_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    estimated_price_in_kroner = table.Column<decimal>(type: "money", nullable: false),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedules", x => x.id);
                    table.ForeignKey(
                        name: "FK_schedules_AspNetUsers_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_schedules_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_schedules_schedules_next_schedule_id",
                        column: x => x.next_schedule_id,
                        principalTable: "schedules",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_presets_device_id",
                table: "presets",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "IX_schedules_device_id",
                table: "schedules",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "IX_schedules_next_schedule_id",
                table: "schedules",
                column: "next_schedule_id");

            migrationBuilder.CreateIndex(
                name: "IX_schedules_user_id",
                table: "schedules",
                column: "user_id");
        }
    }
}
