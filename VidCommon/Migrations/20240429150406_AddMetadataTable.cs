using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VidCommon.Migrations
{
    /// <inheritdoc />
    public partial class AddMetadataTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoogleVideosMetadataFromChrome",
                columns: table => new
                {
                    ProductId = table.Column<string>(type: "TEXT", nullable: false),
                    Filesize = table.Column<int>(type: "INTEGER", nullable: true),
                    Filename = table.Column<string>(type: "TEXT", nullable: true),
                    RestId = table.Column<string>(type: "TEXT", nullable: true),
                    DurationMs = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleVideosMetadataFromChrome", x => x.ProductId);
                });

            migrationBuilder.CreateTable(
                name: "WindowsVideos",
                columns: table => new
                {
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    Folder = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Duration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    DurationMs = table.Column<long>(type: "INTEGER", nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WindowsVideos", x => x.Path);
                });

            migrationBuilder.CreateTable(
                name: "GoogleVideos",
                columns: table => new
                {
                    GoogleId = table.Column<string>(type: "TEXT", nullable: false),
                    URL = table.Column<string>(type: "TEXT", nullable: true),
                    VideoMetadataFromChromeProductId = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Duration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    DurationMs = table.Column<long>(type: "INTEGER", nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleVideos", x => x.GoogleId);
                    table.ForeignKey(
                        name: "FK_GoogleVideos_GoogleVideosMetadataFromChrome_VideoMetadataFromChromeProductId",
                        column: x => x.VideoMetadataFromChromeProductId,
                        principalTable: "GoogleVideosMetadataFromChrome",
                        principalColumn: "ProductId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoogleVideos_VideoMetadataFromChromeProductId",
                table: "GoogleVideos",
                column: "VideoMetadataFromChromeProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoogleVideos");

            migrationBuilder.DropTable(
                name: "WindowsVideos");

            migrationBuilder.DropTable(
                name: "GoogleVideosMetadataFromChrome");
        }
    }
}
