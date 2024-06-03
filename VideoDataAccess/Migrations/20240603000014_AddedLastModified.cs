using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VideoDataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddedLastModified : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastModified",
                table: "WindowsVideos",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastModified",
                table: "WindowsVideos");
        }
    }
}
