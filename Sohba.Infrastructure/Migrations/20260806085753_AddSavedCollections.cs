using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sohba.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CollectionId",
                table: "SavedPost",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "SavedPost",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "SavedCollections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsFavorites = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedCollections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedCollections_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedPost_CollectionId",
                table: "SavedPost",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedCollections_UserId",
                table: "SavedCollections",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SavedPost_SavedCollections_CollectionId",
                table: "SavedPost",
                column: "CollectionId",
                principalTable: "SavedCollections",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SavedPost_SavedCollections_CollectionId",
                table: "SavedPost");

            migrationBuilder.DropTable(
                name: "SavedCollections");

            migrationBuilder.DropIndex(
                name: "IX_SavedPost_CollectionId",
                table: "SavedPost");

            migrationBuilder.DropColumn(
                name: "CollectionId",
                table: "SavedPost");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "SavedPost");
        }
    }
}
