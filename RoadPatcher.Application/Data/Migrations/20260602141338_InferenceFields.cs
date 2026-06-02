using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadPatcher.Application.Data.Migrations
{
    /// <inheritdoc />
    public partial class InferenceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InferenceData",
                table: "Posts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InferenceImageId",
                table: "Posts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_InferenceImageId",
                table: "Posts",
                column: "InferenceImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Media_InferenceImageId",
                table: "Posts",
                column: "InferenceImageId",
                principalTable: "Media",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Media_InferenceImageId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_InferenceImageId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "InferenceData",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "InferenceImageId",
                table: "Posts");
        }
    }
}
