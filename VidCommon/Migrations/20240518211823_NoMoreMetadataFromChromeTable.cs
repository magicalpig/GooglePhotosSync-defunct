using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VidCommon.Migrations
{
    /// <inheritdoc />
    public partial class NoMoreMetadataFromChromeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoogleVideosMetadataFromChrome");

            migrationBuilder.AlterColumn<long>(
                name: "Size",
                table: "WindowsVideos",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<long>(
                name: "DurationMs",
                table: "WindowsVideos",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "Duration",
                table: "WindowsVideos",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(TimeSpan),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<long>(
                name: "Size",
                table: "GoogleVideos",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<long>(
                name: "DurationMs",
                table: "GoogleVideos",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "Duration",
                table: "GoogleVideos",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(TimeSpan),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "RPCId",
                table: "GoogleVideos",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RPCId",
                table: "GoogleVideos");

            migrationBuilder.AlterColumn<long>(
                name: "Size",
                table: "WindowsVideos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "DurationMs",
                table: "WindowsVideos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "Duration",
                table: "WindowsVideos",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0),
                oldClrType: typeof(TimeSpan),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "Size",
                table: "GoogleVideos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "DurationMs",
                table: "GoogleVideos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "Duration",
                table: "GoogleVideos",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0),
                oldClrType: typeof(TimeSpan),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "GoogleVideosMetadataFromChrome",
                columns: table => new
                {
                    ProductId = table.Column<string>(type: "TEXT", nullable: false),
                    DurationMs = table.Column<int>(type: "INTEGER", nullable: true),
                    Filename = table.Column<string>(type: "TEXT", nullable: true),
                    Filesize = table.Column<int>(type: "INTEGER", nullable: true),
                    RestId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleVideosMetadataFromChrome", x => x.ProductId);
                });
        }
    }
}
