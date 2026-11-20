using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartHomeApplicationAPI.Migrations;

public partial class AddUserPriceArea : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "PriceArea",
            table: "AspNetUsers",
            type: "character varying(3)",
            maxLength: 3,
            nullable: false,
            defaultValue: "DK1");

        // Backfill existing users only; new registrations must choose their price area.
        migrationBuilder.Sql("ALTER TABLE \"AspNetUsers\" ALTER COLUMN \"PriceArea\" DROP DEFAULT;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "PriceArea", table: "AspNetUsers");
    }
}
