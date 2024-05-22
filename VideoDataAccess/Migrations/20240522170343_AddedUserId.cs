using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoDataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddedUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "URL",
                table: "GoogleVideos",
                newName: "Url");

            migrationBuilder.RenameColumn(
                name: "RPCId",
                table: "GoogleVideos",
                newName: "RpcId");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "WindowsVideos");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "GoogleVideos");

            migrationBuilder.RenameColumn(
                name: "Url",
                table: "GoogleVideos",
                newName: "URL");

            migrationBuilder.RenameColumn(
                name: "RpcId",
                table: "GoogleVideos",
                newName: "RPCId");
        }
    }
}
