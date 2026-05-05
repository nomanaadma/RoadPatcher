using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadPatcher.Application.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowEdit",
                table: "Posts");

            migrationBuilder.AddColumn<int>(
                name: "ReviewStatus",
                table: "Posts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReviewStatus",
                table: "Posts");

            migrationBuilder.AddColumn<bool>(
                name: "AllowEdit",
                table: "Posts",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }
    }
}
