using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sohba.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixCommentsAndSavedPosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SavedPost_SavedCollections_CollectionId",
                table: "SavedPost");

            migrationBuilder.DropIndex(
                name: "IX_SavedPost_UserId",
                table: "SavedPost");

            migrationBuilder.DropIndex(
                name: "IX_Reactions_PostId",
                table: "Reactions");

            migrationBuilder.CreateIndex(
                name: "IX_SavedPost_UserId_PostId_CollectionId",
                table: "SavedPost",
                columns: new[] { "UserId", "PostId", "CollectionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_PostId_UserId",
                table: "Reactions",
                columns: new[] { "PostId", "UserId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SavedPost_SavedCollections_CollectionId",
                table: "SavedPost",
                column: "CollectionId",
                principalTable: "SavedCollections",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SavedPost_SavedCollections_CollectionId",
                table: "SavedPost");

            migrationBuilder.DropIndex(
                name: "IX_SavedPost_UserId_PostId_CollectionId",
                table: "SavedPost");

            migrationBuilder.DropIndex(
                name: "IX_Reactions_PostId_UserId",
                table: "Reactions");

            migrationBuilder.CreateIndex(
                name: "IX_SavedPost_UserId",
                table: "SavedPost",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_PostId",
                table: "Reactions",
                column: "PostId");

            migrationBuilder.AddForeignKey(
                name: "FK_SavedPost_SavedCollections_CollectionId",
                table: "SavedPost",
                column: "CollectionId",
                principalTable: "SavedCollections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
