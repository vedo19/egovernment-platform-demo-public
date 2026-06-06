using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CitizenService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExtendedCitizenProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Citizenship",
                table: "CitizenProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "CitizenProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "CitizenProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PlaceOfBirth",
                table: "CitizenProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PlaceOfResidence",
                table: "CitizenProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ZipCode",
                table: "CitizenProfiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Citizenship",
                table: "CitizenProfiles");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "CitizenProfiles");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "CitizenProfiles");

            migrationBuilder.DropColumn(
                name: "PlaceOfBirth",
                table: "CitizenProfiles");

            migrationBuilder.DropColumn(
                name: "PlaceOfResidence",
                table: "CitizenProfiles");

            migrationBuilder.DropColumn(
                name: "ZipCode",
                table: "CitizenProfiles");
        }
    }
}
