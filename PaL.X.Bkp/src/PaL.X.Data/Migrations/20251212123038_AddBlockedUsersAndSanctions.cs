using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PaL.X.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockedUsersAndSanctions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BloquedUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    BlockedByUserId = table.Column<int>(type: "integer", nullable: false),
                    BlockedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    BlockedUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationDays = table.Column<int>(type: "integer", nullable: true),
                    IsPermanent = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Gender = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BloquedUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BloquedUsers_Users_BlockedByUserId",
                        column: x => x.BlockedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BloquedUsers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSanctionHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    BlockedByUserId = table.Column<int>(type: "integer", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSanctionHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSanctionHistory_Users_BlockedByUserId",
                        column: x => x.BlockedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSanctionHistory_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BloquedUsers_BlockedByUserId",
                table: "BloquedUsers",
                column: "BlockedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BloquedUsers_UserId",
                table: "BloquedUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BloquedUsers_UserId_BlockedByUserId",
                table: "BloquedUsers",
                columns: new[] { "UserId", "BlockedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSanctionHistory_BlockedByUserId",
                table: "UserSanctionHistory",
                column: "BlockedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSanctionHistory_UserId",
                table: "UserSanctionHistory",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BloquedUsers");

            migrationBuilder.DropTable(
                name: "UserSanctionHistory");
        }
    }
}
