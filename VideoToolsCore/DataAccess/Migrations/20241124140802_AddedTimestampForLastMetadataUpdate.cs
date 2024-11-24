using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoToolsCore.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddedTimestampForLastMetadataUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastMetadataScanTimestamp",
                table: "WindowsVideos",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastMetadataScanTimestamp",
                table: "GoogleVideos",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastMetadataScanTimestamp",
                table: "WindowsVideos");

            migrationBuilder.DropColumn(
                name: "LastMetadataScanTimestamp",
                table: "GoogleVideos");
        }
    }
}
