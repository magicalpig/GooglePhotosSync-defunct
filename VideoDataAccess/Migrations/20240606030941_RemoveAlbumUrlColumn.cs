using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoDataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAlbumUrlColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Url",
                table: "GoogleAlbums");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "GoogleAlbums",
                type: "TEXT",
                nullable: true);
        }
    }
}
