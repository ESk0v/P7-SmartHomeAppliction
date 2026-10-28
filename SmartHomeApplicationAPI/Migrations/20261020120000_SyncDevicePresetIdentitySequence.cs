using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SmartHomeApplicationAPI.Infrastructure;

#nullable disable

namespace SmartHomeApplicationAPI.Migrations
{
    [DbContext(typeof(SmartHomeDbContext))]
    [Migration("20261020120000_SyncDevicePresetIdentitySequence")]
    public partial class SyncDevicePresetIdentitySequence : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                SELECT setval(
                    pg_get_serial_sequence('"device_preset"', 'id'),
                    COALESCE((SELECT MAX(id) FROM "device_preset"), 1),
                    EXISTS (SELECT 1 FROM "device_preset")
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }

        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            new SnapshotModelBuilder().Build(modelBuilder);
        }

        private sealed class SnapshotModelBuilder : SmartHomeDbContextModelSnapshot
        {
            public void Build(ModelBuilder modelBuilder) => BuildModel(modelBuilder);
        }
    }
}
