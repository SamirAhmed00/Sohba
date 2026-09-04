using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sohba.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PageRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                table: "Pages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Rules",
                table: "Pages",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PageFollowRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    PageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageFollowRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PageFollowRequests_AspNetUsers_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PageFollowRequests_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PageFollowRequests_Pages_PageId",
                        column: x => x.PageId,
                        principalTable: "Pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PageFollowRequests_PageId_Status",
                table: "PageFollowRequests",
                columns: new[] { "PageId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PageFollowRequests_PageId_UserId",
                table: "PageFollowRequests",
                columns: new[] { "PageId", "UserId" },
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PageFollowRequests_ReviewedByUserId",
                table: "PageFollowRequests",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PageFollowRequests_UserId",
                table: "PageFollowRequests",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PageFollowRequests");

            migrationBuilder.DropColumn(
                name: "IsPrivate",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "Rules",
                table: "Pages");
        }
    }
}
