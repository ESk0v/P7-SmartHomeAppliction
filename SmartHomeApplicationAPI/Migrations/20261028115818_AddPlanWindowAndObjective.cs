using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartHomeApplicationAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanWindowAndObjective : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "start_time",
                table: "plan",
                newName: "plan_start_time");

            migrationBuilder.RenameColumn(
                name: "end_time",
                table: "plan",
                newName: "plan_end_time");

            migrationBuilder.RenameColumn(
                name: "estimated_co2_emissions_in_percent",
                table: "plan",
                newName: "estimated_co2_emissions_in_grams");

            migrationBuilder.AddColumn<decimal>(
                name: "estimated_unscheduled_co2_emissions_in_grams",
                table: "plan",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "optimization_objective",
                table: "plan",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "window_end_time",
                table: "plan",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "window_start_time",
                table: "plan",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE plan
                SET window_start_time = plan_start_time,
                    window_end_time = plan_end_time,
                    estimated_unscheduled_co2_emissions_in_grams = 30,
                    optimization_objective = 'Cheapest';
                """);

            migrationBuilder.AlterColumn<decimal>(
                name: "estimated_unscheduled_co2_emissions_in_grams",
                table: "plan",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "optimization_objective",
                table: "plan",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "window_end_time",
                table: "plan",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "window_start_time",
                table: "plan",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "optimization_objective",
                table: "plan");

            migrationBuilder.DropColumn(
                name: "estimated_unscheduled_co2_emissions_in_grams",
                table: "plan");

            migrationBuilder.DropColumn(
                name: "window_end_time",
                table: "plan");

            migrationBuilder.DropColumn(
                name: "window_start_time",
                table: "plan");

            migrationBuilder.RenameColumn(
                name: "plan_start_time",
                table: "plan",
                newName: "start_time");

            migrationBuilder.RenameColumn(
                name: "plan_end_time",
                table: "plan",
                newName: "end_time");

            migrationBuilder.RenameColumn(
                name: "estimated_co2_emissions_in_grams",
                table: "plan",
                newName: "estimated_co2_emissions_in_percent");
        }
    }
}
