using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadPatcher.Application.Data.Migrations
{
    /// <inheritdoc />
    public partial class PostComplete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompletionImageId",
                table: "Posts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_CompletionImageId",
                table: "Posts",
                column: "CompletionImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Media_CompletionImageId",
                table: "Posts",
                column: "CompletionImageId",
                principalTable: "Media",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Media_CompletionImageId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_CompletionImageId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "CompletionImageId",
                table: "Posts");
        }
    }
}
