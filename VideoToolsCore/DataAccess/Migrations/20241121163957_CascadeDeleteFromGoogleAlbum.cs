using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoToolsCore.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CascadeDeleteFromGoogleAlbum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AlbumMemberships_GoogleAlbums_GoogleAlbumId_Owner",
                table: "AlbumMemberships");

            migrationBuilder.AddForeignKey(
                name: "FK_AlbumMemberships_GoogleAlbums_GoogleAlbumId_Owner",
                table: "AlbumMemberships",
                columns: new[] { "GoogleAlbumId", "Owner" },
                principalTable: "GoogleAlbums",
                principalColumns: new[] { "Id", "Owner" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AlbumMemberships_GoogleAlbums_GoogleAlbumId_Owner",
                table: "AlbumMemberships");

            migrationBuilder.AddForeignKey(
                name: "FK_AlbumMemberships_GoogleAlbums_GoogleAlbumId_Owner",
                table: "AlbumMemberships",
                columns: new[] { "GoogleAlbumId", "Owner" },
                principalTable: "GoogleAlbums",
                principalColumns: new[] { "Id", "Owner" });
        }
    }
}
