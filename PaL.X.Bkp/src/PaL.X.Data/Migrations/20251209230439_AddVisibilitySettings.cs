using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaL.X.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVisibilitySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VisibilityCountry",
                table: "UserProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VisibilityDateOfBirth",
                table: "UserProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VisibilityFirstName",
                table: "UserProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VisibilityGender",
                table: "UserProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VisibilityLastName",
                table: "UserProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VisibilityProfilePicture",
                table: "UserProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VisibilityCountry",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "VisibilityDateOfBirth",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "VisibilityFirstName",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "VisibilityGender",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "VisibilityLastName",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "VisibilityProfilePicture",
                table: "UserProfiles");
        }
    }
}
