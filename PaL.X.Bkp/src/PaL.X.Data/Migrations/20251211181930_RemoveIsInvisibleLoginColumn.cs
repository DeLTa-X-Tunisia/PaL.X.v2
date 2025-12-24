using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaL.X.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsInvisibleLoginColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsInvisibleLogin",
                table: "Sessions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsInvisibleLogin",
                table: "Sessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
