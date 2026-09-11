using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sohba.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoUrlToPosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VideoUrl",
                table: "Posts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VideoUrl",
                table: "Posts");
        }
    }
}
