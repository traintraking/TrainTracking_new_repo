using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainTracking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addCountTickits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RewardTicketsCount",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "AppliedDiscount",
                table: "Bookings",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RewardTicketsCount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AppliedDiscount",
                table: "Bookings");
        }
    }
}
