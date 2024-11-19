using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoToolsCore.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddedAlbumsAsManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlbumId",
                table: "GoogleVideos");

            migrationBuilder.CreateTable(
                name: "GoogleAlbums",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Owner = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleAlbums", x => new { x.Id, x.Owner });
                });

            migrationBuilder.CreateTable(
                name: "AlbumMemberships",
                columns: table => new
                {
                    VideoFileGoogleId = table.Column<string>(type: "TEXT", nullable: false),
                    GoogleAlbumId = table.Column<string>(type: "TEXT", nullable: false),
                    Owner = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlbumMemberships", x => new { x.VideoFileGoogleId, x.GoogleAlbumId });
                    table.ForeignKey(
                        name: "FK_AlbumMemberships_GoogleAlbums_GoogleAlbumId_Owner",
                        columns: x => new { x.GoogleAlbumId, x.Owner },
                        principalTable: "GoogleAlbums",
                        principalColumns: new[] { "Id", "Owner" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlbumMemberships_GoogleVideos_VideoFileGoogleId_Owner",
                        columns: x => new { x.VideoFileGoogleId, x.Owner },
                        principalTable: "GoogleVideos",
                        principalColumns: new[] { "GoogleId", "Owner" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlbumMemberships_GoogleAlbumId_Owner",
                table: "AlbumMemberships",
                columns: new[] { "GoogleAlbumId", "Owner" });

            migrationBuilder.CreateIndex(
                name: "IX_AlbumMemberships_VideoFileGoogleId_Owner",
                table: "AlbumMemberships",
                columns: new[] { "VideoFileGoogleId", "Owner" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlbumMemberships");

            migrationBuilder.DropTable(
                name: "GoogleAlbums");

            migrationBuilder.AddColumn<string>(
                name: "AlbumId",
                table: "GoogleVideos",
                type: "TEXT",
                nullable: true);
        }
    }
}
