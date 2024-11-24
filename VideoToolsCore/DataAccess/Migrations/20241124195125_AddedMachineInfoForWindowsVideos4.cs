using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoToolsCore.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddedMachineInfoForWindowsVideos4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MachineId",
                table: "WindowsVideos",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MachineName",
                table: "WindowsVideos",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MachineId",
                table: "WindowsVideos");

            migrationBuilder.DropColumn(
                name: "MachineName",
                table: "WindowsVideos");
        }
    }
}
