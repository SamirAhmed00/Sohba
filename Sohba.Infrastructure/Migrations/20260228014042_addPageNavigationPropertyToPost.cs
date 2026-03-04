using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sohba.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addPageNavigationPropertyToPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PageId",
                table: "Posts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_PageId",
                table: "Posts",
                column: "PageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Pages_PageId",
                table: "Posts",
                column: "PageId",
                principalTable: "Pages",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Pages_PageId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_PageId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "PageId",
                table: "Posts");
        }
    }
}
