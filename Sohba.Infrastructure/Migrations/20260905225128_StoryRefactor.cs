using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sohba.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StoryRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoryViewer_StoryId",
                table: "StoryViewer");

            migrationBuilder.DropIndex(
                name: "IX_Stories_UserId",
                table: "Stories");

            migrationBuilder.CreateIndex(
                name: "IX_StoryViewer_StoryId_UserId",
                table: "StoryViewer",
                columns: new[] { "StoryId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stories_ExpiresAt_IsDeleted",
                table: "Stories",
                columns: new[] { "ExpiresAt", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Stories_UserId_ExpiresAt",
                table: "Stories",
                columns: new[] { "UserId", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoryViewer_StoryId_UserId",
                table: "StoryViewer");

            migrationBuilder.DropIndex(
                name: "IX_Stories_ExpiresAt_IsDeleted",
                table: "Stories");

            migrationBuilder.DropIndex(
                name: "IX_Stories_UserId_ExpiresAt",
                table: "Stories");

            migrationBuilder.CreateIndex(
                name: "IX_StoryViewer_StoryId",
                table: "StoryViewer",
                column: "StoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Stories_UserId",
                table: "Stories",
                column: "UserId");
        }
    }
}
