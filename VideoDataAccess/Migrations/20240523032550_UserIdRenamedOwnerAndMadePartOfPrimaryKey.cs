using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoDataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UserIdRenamedOwnerAndMadePartOfPrimaryKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_WindowsVideos",
                table: "WindowsVideos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GoogleVideos",
                table: "GoogleVideos");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "WindowsVideos");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "GoogleVideos");

            migrationBuilder.AddColumn<string>(
                name: "Owner",
                table: "WindowsVideos",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Owner",
                table: "GoogleVideos",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WindowsVideos",
                table: "WindowsVideos",
                columns: new[] { "Path", "Owner" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_GoogleVideos",
                table: "GoogleVideos",
                columns: new[] { "GoogleId", "Owner" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_WindowsVideos",
                table: "WindowsVideos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GoogleVideos",
                table: "GoogleVideos");

            migrationBuilder.DropColumn(
                name: "Owner",
                table: "WindowsVideos");

            migrationBuilder.DropColumn(
                name: "Owner",
                table: "GoogleVideos");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "WindowsVideos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "GoogleVideos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_WindowsVideos",
                table: "WindowsVideos",
                column: "Path");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GoogleVideos",
                table: "GoogleVideos",
                column: "GoogleId");
        }
    }
}
