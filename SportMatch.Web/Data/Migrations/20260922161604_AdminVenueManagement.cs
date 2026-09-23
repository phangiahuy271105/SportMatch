using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportMatch.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdminVenueManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "VenueComplexes",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "VenueComplexes",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "VenueComplexes");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "VenueComplexes");
        }
    }
}
