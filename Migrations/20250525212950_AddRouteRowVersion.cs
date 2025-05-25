using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // Prefer 'static readonly' fields over constant array arguments

namespace BusBus.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Routes",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: Array.Empty<byte>());

            migrationBuilder.InsertData(
                table: "Drivers",
                columns: new[] { "Id", "Email", "FirstName", "LastName", "LicenseNumber", "Name", "PhoneNumber" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), null, "John", "Smith", "DL123456", "John Smith", null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), null, "Mary", "Johnson", "DL234567", "Mary Johnson", null },
                    { new Guid("33333333-3333-3333-3333-333333333333"), null, "Robert", "Brown", "DL345678", "Robert Brown", null },
                    { new Guid("44444444-4444-4444-4444-444444444444"), null, "Lisa", "Davis", "DL456789", "Lisa Davis", null },
                    { new Guid("55555555-5555-5555-5555-555555555555"), null, "Michael", "Wilson", "DL567890", "Michael Wilson", null }
                });

            migrationBuilder.InsertData(
                table: "Vehicles",
                columns: new[] { "Id", "BusNumber", "Capacity", "IsActive", "LicensePlate", "Model", "Name", "Number" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "101", 72, true, "BUS-101", "Blue Bird All American FE", null, "101" },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "102", 66, true, "BUS-102", "Thomas C2 Jouley", null, "102" },
                    { new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), "103", 78, true, "BUS-103", "IC Bus CE Series", null, "103" },
                    { new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"), "104", 72, false, "BUS-104", "Blue Bird Vision", null, "104" },
                    { new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), "105", 90, true, "BUS-105", "Thomas HDX", null, "105" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);
            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"));

            migrationBuilder.DeleteData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"));

            migrationBuilder.DeleteData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"));

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Routes");
        }
    }
}
