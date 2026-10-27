using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SmartHomeApplicationAPI.Migrations
{
    /// <inheritdoc />
    public partial class CreateElectricityTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "electricity",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    time = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: false),
                    price_per_kwh = table.Column<decimal>(type: "numeric(10,5)", nullable: false),
                    price_area = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    is_predicted = table.Column<bool>(type: "boolean", nullable: false),
                    co2_emission_g_per_kwh = table.Column<decimal>(type: "numeric(10,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_electricity", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "electricity");

            migrationBuilder.CreateTable(
                name: "day_ahead_price",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    is_predicted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    price_area = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    time = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("day_ahead_price_pkey", x => x.id);
                });
        }
    }
}
