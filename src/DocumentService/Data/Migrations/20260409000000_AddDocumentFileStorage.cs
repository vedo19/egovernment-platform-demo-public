using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentFileStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "FileContent",
                table: "Documents",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "Documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GeneratedAt",
                table: "Documents",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileContent",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "GeneratedAt",
                table: "Documents");
        }
    }
}
