using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowFieldsToDocumentRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedOfficerId",
                table: "Documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SupportingDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CitizenUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileData = table.Column<byte[]>(type: "bytea", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportingDocuments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupportingDocuments_CitizenUserId",
                table: "SupportingDocuments",
                column: "CitizenUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportingDocuments_ServiceRequestId",
                table: "SupportingDocuments",
                column: "ServiceRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportingDocuments_UploadedAt",
                table: "SupportingDocuments",
                column: "UploadedAt");

            migrationBuilder.Sql("""
                UPDATE "Documents"
                SET "AssignedOfficerId" = "ProcessedByOfficerId"
                WHERE "AssignedOfficerId" IS NULL AND "ProcessedByOfficerId" IS NOT NULL;

                UPDATE "Documents"
                SET "Status" = 'Submitted'
                WHERE "Status" = 'Pending';

                UPDATE "Documents"
                SET "Status" = 'UnderReview'
                WHERE "Status" = 'Processing';

                UPDATE "Documents"
                SET "Status" = 'Approved'
                WHERE "Status" IN ('Ready', 'Collected');
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Documents"
                SET "Status" = 'Pending'
                WHERE "Status" = 'Submitted';

                UPDATE "Documents"
                SET "Status" = 'Processing'
                WHERE "Status" = 'UnderReview';

                UPDATE "Documents"
                SET "Status" = 'Ready'
                WHERE "Status" = 'Approved';
            """);

            migrationBuilder.DropTable(
                name: "SupportingDocuments");

            migrationBuilder.DropColumn(
                name: "AssignedOfficerId",
                table: "Documents");
        }
    }
}
