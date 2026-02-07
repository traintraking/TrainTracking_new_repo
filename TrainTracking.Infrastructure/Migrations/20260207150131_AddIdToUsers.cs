using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainTracking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIdToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NationalId",
                table: "Users",
                type: "TEXT",
                maxLength: 14,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NationalId",
                table: "Users");
        }
    }
}
