using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartHomeApplicationAPI.Migrations
{
    public partial class PlanTimestampsWithoutTimeZone : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE plan
                    ALTER COLUMN plan_start_time TYPE timestamp without time zone
                        USING plan_start_time AT TIME ZONE 'UTC',
                    ALTER COLUMN plan_end_time TYPE timestamp without time zone
                        USING plan_end_time AT TIME ZONE 'UTC',
                    ALTER COLUMN window_start_time TYPE timestamp without time zone
                        USING window_start_time AT TIME ZONE 'UTC',
                    ALTER COLUMN window_end_time TYPE timestamp without time zone
                        USING window_end_time AT TIME ZONE 'UTC';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE plan
                    ALTER COLUMN plan_start_time TYPE timestamp with time zone
                        USING plan_start_time AT TIME ZONE 'UTC',
                    ALTER COLUMN plan_end_time TYPE timestamp with time zone
                        USING plan_end_time AT TIME ZONE 'UTC',
                    ALTER COLUMN window_start_time TYPE timestamp with time zone
                        USING window_start_time AT TIME ZONE 'UTC',
                    ALTER COLUMN window_end_time TYPE timestamp with time zone
                        USING window_end_time AT TIME ZONE 'UTC';
                """);
        }
    }
}
