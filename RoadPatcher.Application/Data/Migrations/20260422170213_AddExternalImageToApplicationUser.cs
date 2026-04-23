using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoadPatcher.Application.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalImageToApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalImage",
                table: "AspNetUsers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalImage",
                table: "AspNetUsers");
        }
    }
}
