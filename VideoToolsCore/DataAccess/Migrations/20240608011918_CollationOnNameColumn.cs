using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoToolsCore.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class CollationOnNameColumn : Migration
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

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "WindowsVideos",
                type: "TEXT",
                nullable: true,
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "GoogleVideos",
                type: "TEXT",
                nullable: true,
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Owner",
                table: "AlbumMemberships",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddForeignKey(
                name: "FK_AlbumMemberships_GoogleAlbums_GoogleAlbumId_Owner",
                table: "AlbumMemberships",
                columns: new[] { "GoogleAlbumId", "Owner" },
                principalTable: "GoogleAlbums",
                principalColumns: new[] { "Id", "Owner" });

            migrationBuilder.AddForeignKey(
                name: "FK_AlbumMemberships_GoogleVideos_VideoFileGoogleId_Owner",
                table: "AlbumMemberships",
                columns: new[] { "VideoFileGoogleId", "Owner" },
                principalTable: "GoogleVideos",
                principalColumns: new[] { "GoogleId", "Owner" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AlbumMemberships_GoogleAlbums_GoogleAlbumId_Owner",
                table: "AlbumMemberships");

            migrationBuilder.DropForeignKey(
                name: "FK_AlbumMemberships_GoogleVideos_VideoFileGoogleId_Owner",
                table: "AlbumMemberships");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "WindowsVideos",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true,
                oldCollation: "NOCASE");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "GoogleVideos",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true,
                oldCollation: "NOCASE");

            migrationBuilder.AlterColumn<string>(
                name: "Owner",
                table: "AlbumMemberships",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

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
                principalColumns: new[] { "GoogleId", "Owner" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
