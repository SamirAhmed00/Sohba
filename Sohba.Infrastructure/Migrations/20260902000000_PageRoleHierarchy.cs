using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sohba.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PageRoleHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Schema change: PageFollowers.PageId FK switches from Restrict to Cascade
            // so that hard-deleting a Page cleans up its follower rows.
            migrationBuilder.DropForeignKey(
                name: "FK_PageFollowers_Pages_PageId",
                table: "PageFollowers");

            migrationBuilder.AddForeignKey(
                name: "FK_PageFollowers_Pages_PageId",
                table: "PageFollowers",
                column: "PageId",
                principalTable: "Pages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Data remap: promote the founder (PageFollower row where UserId == Page.AdminId) to PageOwner (4),
            // then collapse all remaining old-Admin rows (2) to the new Admin value (3).
            // Member rows (1) untouched.
            migrationBuilder.Sql(@"
                UPDATE pf
                SET pf.Role = 4
                FROM PageFollowers pf
                INNER JOIN Pages p ON p.Id = pf.PageId
                WHERE pf.UserId = p.AdminId;
            ");

            migrationBuilder.Sql(@"
                UPDATE PageFollowers
                SET Role = 3
                WHERE Role = 2;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse: PageOwner (4) and Admin (3) collapse back to old Admin (2).
            migrationBuilder.Sql(@"
                UPDATE PageFollowers
                SET Role = 2
                WHERE Role IN (3, 4);
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_PageFollowers_Pages_PageId",
                table: "PageFollowers");

            migrationBuilder.AddForeignKey(
                name: "FK_PageFollowers_Pages_PageId",
                table: "PageFollowers",
                column: "PageId",
                principalTable: "Pages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
