using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoToolsCore.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddedCodecForWindows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Codec",
                table: "WindowsVideos",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Codec",
                table: "WindowsVideos");
        }
    }
}
