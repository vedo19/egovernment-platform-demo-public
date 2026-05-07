using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceRequestService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowFieldsToServiceRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LinkedDocumentId",
                table: "ServiceRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfficerNote",
                table: "ServiceRequests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRequests_LinkedDocumentId",
                table: "ServiceRequests",
                column: "LinkedDocumentId");

            migrationBuilder.Sql("""
                UPDATE "ServiceRequests"
                SET "Status" = 'Submitted'
                WHERE "Status" = 'Pending';

                UPDATE "ServiceRequests"
                SET "Status" = 'UnderReview'
                WHERE "Status" = 'InProgress';

                UPDATE "ServiceRequests"
                SET "Status" = 'Approved'
                WHERE "Status" = 'Resolved';
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "ServiceRequests"
                SET "Status" = 'Pending'
                WHERE "Status" = 'Submitted';

                UPDATE "ServiceRequests"
                SET "Status" = 'InProgress'
                WHERE "Status" = 'UnderReview';

                UPDATE "ServiceRequests"
                SET "Status" = 'Resolved'
                WHERE "Status" = 'Approved';
            """);

            migrationBuilder.DropIndex(
                name: "IX_ServiceRequests_LinkedDocumentId",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "LinkedDocumentId",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "OfficerNote",
                table: "ServiceRequests");
        }
    }
}
