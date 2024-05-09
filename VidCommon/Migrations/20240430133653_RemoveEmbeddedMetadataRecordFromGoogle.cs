using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VidCommon.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEmbeddedMetadataRecordFromGoogle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoogleVideos_GoogleVideosMetadataFromChrome_VideoMetadataFromChromeProductId",
                table: "GoogleVideos");

            migrationBuilder.DropIndex(
                name: "IX_GoogleVideos_VideoMetadataFromChromeProductId",
                table: "GoogleVideos");

            migrationBuilder.DropColumn(
                name: "VideoMetadataFromChromeProductId",
                table: "GoogleVideos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VideoMetadataFromChromeProductId",
                table: "GoogleVideos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoogleVideos_VideoMetadataFromChromeProductId",
                table: "GoogleVideos",
                column: "VideoMetadataFromChromeProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_GoogleVideos_GoogleVideosMetadataFromChrome_VideoMetadataFromChromeProductId",
                table: "GoogleVideos",
                column: "VideoMetadataFromChromeProductId",
                principalTable: "GoogleVideosMetadataFromChrome",
                principalColumn: "ProductId");
        }
    }
}
