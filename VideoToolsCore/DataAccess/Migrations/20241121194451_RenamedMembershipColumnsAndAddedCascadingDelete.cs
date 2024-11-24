using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoToolsCore.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenamedMembershipColumnsAndAddedCascadingDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AlbumMemberships_GoogleAlbums_GoogleAlbumId_Owner",
                table: "AlbumMemberships");

            migrationBuilder.DropForeignKey(
                name: "FK_AlbumMemberships_GoogleVideos_VideoFileGoogleId_Owner",
                table: "AlbumMemberships");

            migrationBuilder.RenameColumn(
                name: "GoogleAlbumId",
                table: "AlbumMemberships",
                newName: "AlbumId");

            migrationBuilder.RenameColumn(
                name: "VideoFileGoogleId",
                table: "AlbumMemberships",
                newName: "VideoId");

            migrationBuilder.RenameIndex(
                name: "IX_AlbumMemberships_VideoFileGoogleId_Owner",
                table: "AlbumMemberships",
                newName: "IX_AlbumMemberships_VideoId_Owner");

            migrationBuilder.RenameIndex(
                name: "IX_AlbumMemberships_GoogleAlbumId_Owner",
                table: "AlbumMemberships",
                newName: "IX_AlbumMemberships_AlbumId_Owner");

            migrationBuilder.AddForeignKey(
                name: "FK_AlbumMemberships_GoogleAlbums_AlbumId_Owner",
                table: "AlbumMemberships",
                columns: new[] { "AlbumId", "Owner" },
                principalTable: "GoogleAlbums",
                principalColumns: new[] { "Id", "Owner" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AlbumMemberships_GoogleVideos_VideoId_Owner",
                table: "AlbumMemberships",
                columns: new[] { "VideoId", "Owner" },
                principalTable: "GoogleVideos",
                principalColumns: new[] { "GoogleId", "Owner" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AlbumMemberships_GoogleAlbums_AlbumId_Owner",
                table: "AlbumMemberships");

            migrationBuilder.DropForeignKey(
                name: "FK_AlbumMemberships_GoogleVideos_VideoId_Owner",
                table: "AlbumMemberships");

            migrationBuilder.RenameColumn(
                name: "AlbumId",
                table: "AlbumMemberships",
                newName: "GoogleAlbumId");

            migrationBuilder.RenameColumn(
                name: "VideoId",
                table: "AlbumMemberships",
                newName: "VideoFileGoogleId");

            migrationBuilder.RenameIndex(
                name: "IX_AlbumMemberships_VideoId_Owner",
                table: "AlbumMemberships",
                newName: "IX_AlbumMemberships_VideoFileGoogleId_Owner");

            migrationBuilder.RenameIndex(
                name: "IX_AlbumMemberships_AlbumId_Owner",
                table: "AlbumMemberships",
                newName: "IX_AlbumMemberships_GoogleAlbumId_Owner");

            migrationBuilder.AddForeignKey(
                name: "FK_AlbumMemberships_GoogleAlbums_GoogleAlbumId_Owner",
                table: "AlbumMemberships",
                columns: new[] { "GoogleAlbumId", "Owner" },
                principalTable: "GoogleAlbums",
                principalColumns: new[] { "Id", "Owner" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AlbumMemberships_GoogleVideos_VideoFileGoogleId_Owner",
                table: "AlbumMemberships",
                columns: new[] { "VideoFileGoogleId", "Owner" },
                principalTable: "GoogleVideos",
                principalColumns: new[] { "GoogleId", "Owner" });
        }
    }
}
