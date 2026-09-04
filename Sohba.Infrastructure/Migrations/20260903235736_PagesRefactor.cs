using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sohba.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PagesRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PageId1",
                table: "PageFollowers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PageFollowers_PageId1",
                table: "PageFollowers",
                column: "PageId1");

            migrationBuilder.AddForeignKey(
                name: "FK_PageFollowers_Pages_PageId1",
                table: "PageFollowers",
                column: "PageId1",
                principalTable: "Pages",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PageFollowers_Pages_PageId1",
                table: "PageFollowers");

            migrationBuilder.DropIndex(
                name: "IX_PageFollowers_PageId1",
                table: "PageFollowers");

            migrationBuilder.DropColumn(
                name: "PageId1",
                table: "PageFollowers");
        }
    }
}
