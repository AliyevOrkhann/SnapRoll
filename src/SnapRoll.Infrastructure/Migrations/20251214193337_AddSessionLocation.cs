using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnapRoll.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Sessions",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Sessions",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxDistanceMeters",
                table: "Sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "MaxDistanceMeters",
                table: "Sessions");
        }
    }
}
