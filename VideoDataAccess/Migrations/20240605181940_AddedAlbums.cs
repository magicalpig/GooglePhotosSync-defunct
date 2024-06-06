using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoDataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddedAlbums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlbumId",
                table: "GoogleVideos",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlbumId",
                table: "GoogleVideos");
        }
    }
}
