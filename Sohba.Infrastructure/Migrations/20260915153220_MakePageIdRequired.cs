using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sohba.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakePageIdRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SavedPost_Posts_PostId1",
                table: "SavedPost");

            migrationBuilder.DropIndex(
                name: "IX_SavedPost_PostId1",
                table: "SavedPost");

            migrationBuilder.DropIndex(
                name: "IX_SavedPost_UserId_PostId_CollectionId",
                table: "SavedPost");

            migrationBuilder.DropColumn(
                name: "PostId1",
                table: "SavedPost");

            migrationBuilder.CreateIndex(
                name: "IX_SavedPost_UserId_PostId_CollectionId",
                table: "SavedPost",
                columns: new[] { "UserId", "PostId", "CollectionId" },
                unique: true,
                filter: "[CollectionId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SavedPost_UserId_PostId_CollectionId",
                table: "SavedPost");

            migrationBuilder.AddColumn<Guid>(
                name: "PostId1",
                table: "SavedPost",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedPost_PostId1",
                table: "SavedPost",
                column: "PostId1");

            migrationBuilder.CreateIndex(
                name: "IX_SavedPost_UserId_PostId_CollectionId",
                table: "SavedPost",
                columns: new[] { "UserId", "PostId", "CollectionId" });

            migrationBuilder.AddForeignKey(
                name: "FK_SavedPost_Posts_PostId1",
                table: "SavedPost",
                column: "PostId1",
                principalTable: "Posts",
                principalColumn: "Id");
        }
    }
}
