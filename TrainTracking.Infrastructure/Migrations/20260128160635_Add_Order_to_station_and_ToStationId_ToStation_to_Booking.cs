using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrainTracking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_Order_to_station_and_ToStationId_ToStation_to_Booking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "Stations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "FromStationId",
                table: "Bookings",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ToStationId",
                table: "Bookings",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_FromStationId",
                table: "Bookings",
                column: "FromStationId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ToStationId",
                table: "Bookings",
                column: "ToStationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Stations_FromStationId",
                table: "Bookings",
                column: "FromStationId",
                principalTable: "Stations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Stations_ToStationId",
                table: "Bookings",
                column: "ToStationId",
                principalTable: "Stations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Stations_FromStationId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Stations_ToStationId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_FromStationId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ToStationId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "Stations");

            migrationBuilder.DropColumn(
                name: "FromStationId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ToStationId",
                table: "Bookings");
        }
    }
}
