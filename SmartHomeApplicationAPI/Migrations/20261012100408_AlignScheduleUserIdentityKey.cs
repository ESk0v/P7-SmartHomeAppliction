using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartHomeApplicationAPI.Migrations
{
    /// <inheritdoc />
    public partial class AlignScheduleUserIdentityKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_schedules_users_user_id",
                table: "schedules");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.Sql(
                """
                ALTER TABLE schedules
                ALTER COLUMN user_id TYPE text
                USING user_id::text;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_schedules_AspNetUsers_user_id",
                table: "schedules",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_schedules_AspNetUsers_user_id",
                table: "schedules");

            migrationBuilder.Sql(
                """
                ALTER TABLE schedules
                ALTER COLUMN user_id TYPE integer
                USING user_id::integer;
                """);

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    TempId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.UniqueConstraint("AK_Users_TempId", x => x.TempId);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_schedules_users_user_id",
                table: "schedules",
                column: "user_id",
                principalTable: "Users",
                principalColumn: "TempId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
