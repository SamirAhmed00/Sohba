using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sohba.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Posts_UserId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Notification_ReceiverId",
                table: "Notification");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_CreatedAt",
                table: "Posts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_SourceType_SourceId",
                table: "Posts",
                columns: new[] { "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_UserId_CreatedAt",
                table: "Posts",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notification_CreatedAt_IsRead",
                table: "Notification",
                columns: new[] { "CreatedAt", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Notification_ReceiverId_CreatedAt",
                table: "Notification",
                columns: new[] { "ReceiverId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notification_ReceiverId_IsRead",
                table: "Notification",
                columns: new[] { "ReceiverId", "IsRead" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Posts_CreatedAt",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_SourceType_SourceId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_UserId_CreatedAt",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Notification_CreatedAt_IsRead",
                table: "Notification");

            migrationBuilder.DropIndex(
                name: "IX_Notification_ReceiverId_CreatedAt",
                table: "Notification");

            migrationBuilder.DropIndex(
                name: "IX_Notification_ReceiverId_IsRead",
                table: "Notification");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_UserId",
                table: "Posts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_ReceiverId",
                table: "Notification",
                column: "ReceiverId");
        }
    }
}
