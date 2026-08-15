using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sohba.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixSavedPostPrimaryKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SavedPost_SavedCollections_CollectionId",
                table: "SavedPost");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SavedPost",
                table: "SavedPost");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SavedPost",
                table: "SavedPost",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_SavedPost_UserId",
                table: "SavedPost",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SavedPost_SavedCollections_CollectionId",
                table: "SavedPost",
                column: "CollectionId",
                principalTable: "SavedCollections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SavedPost_SavedCollections_CollectionId",
                table: "SavedPost");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SavedPost",
                table: "SavedPost");

            migrationBuilder.DropIndex(
                name: "IX_SavedPost_UserId",
                table: "SavedPost");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AspNetUsers");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SavedPost",
                table: "SavedPost",
                columns: new[] { "UserId", "PostId" });

            migrationBuilder.AddForeignKey(
                name: "FK_SavedPost_SavedCollections_CollectionId",
                table: "SavedPost",
                column: "CollectionId",
                principalTable: "SavedCollections",
                principalColumn: "Id");
        }
    }
}
