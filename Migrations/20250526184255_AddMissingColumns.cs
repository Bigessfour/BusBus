#pragma warning disable CA1825 // Avoid unnecessary zero-length array allocations
#pragma warning disable CA1861 // Prefer 'static readonly' fields over constant array arguments
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusBus.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingColumns : Migration
    {        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Vehicles",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "FuelType",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsMaintenanceRequired",
                table: "Vehicles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLocationUpdate",
                table: "Vehicles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastMaintenanceDate",
                table: "Vehicles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Vehicles",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Vehicles",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MaintenanceDue",
                table: "Vehicles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MaintenanceHistoryJson",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MakeModel",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Mileage",
                table: "Vehicles",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "Vehicles",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "NextMaintenanceDate",
                table: "Vehicles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Vehicles",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecificationsJson",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VehicleCode",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VehicleGuid",
                table: "Vehicles",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "VehicleId",
                table: "Vehicles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "Vehicles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Routes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Routes",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Distance",
                table: "Routes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Routes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "Routes",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "RouteCode",
                table: "Routes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RouteID",
                table: "Routes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RouteName",
                table: "Routes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ScheduleJson",
                table: "Routes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StopsJson",
                table: "Routes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactInfo",
                table: "Drivers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Drivers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Drivers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "DriverID",
                table: "Drivers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DriverName",
                table: "Drivers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactJson",
                table: "Drivers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "HireDate",
                table: "Drivers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPerformanceReview",
                table: "Drivers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "Drivers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "PerformanceScore",
                table: "Drivers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PersonalDetails",
                table: "Drivers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Drivers",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "SalaryGrade",
                table: "Drivers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Drivers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "CustomFields",
                columns: table => new
                {
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Required = table.Column<bool>(type: "bit", nullable: false),
                    DefaultValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomFields", x => x.Name);
                });

            migrationBuilder.UpdateData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "ContactInfo", "CreatedBy", "CreatedDate", "DriverID", "DriverName", "EmergencyContactJson", "HireDate", "LastPerformanceReview", "ModifiedDate", "PerformanceScore", "PersonalDetails", "RowVersion", "SalaryGrade", "Status" },
                values: new object[] { "", "", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, "", "", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 5.0m, "", new byte[0], 0, "Active" });

            migrationBuilder.UpdateData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "ContactInfo", "CreatedBy", "CreatedDate", "DriverID", "DriverName", "EmergencyContactJson", "HireDate", "LastPerformanceReview", "ModifiedDate", "PerformanceScore", "PersonalDetails", "RowVersion", "SalaryGrade", "Status" },
                values: new object[] { "", "", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, "", "", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 5.0m, "", new byte[0], 0, "Active" });

            migrationBuilder.UpdateData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "ContactInfo", "CreatedBy", "CreatedDate", "DriverID", "DriverName", "EmergencyContactJson", "HireDate", "LastPerformanceReview", "ModifiedDate", "PerformanceScore", "PersonalDetails", "RowVersion", "SalaryGrade", "Status" },
                values: new object[] { "", "", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, "", "", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 5.0m, "", new byte[0], 0, "Active" });

            migrationBuilder.UpdateData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                columns: new[] { "ContactInfo", "CreatedBy", "CreatedDate", "DriverID", "DriverName", "EmergencyContactJson", "HireDate", "LastPerformanceReview", "ModifiedDate", "PerformanceScore", "PersonalDetails", "RowVersion", "SalaryGrade", "Status" },
                values: new object[] { "", "", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, "", "", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 5.0m, "", new byte[0], 0, "Active" });

            migrationBuilder.UpdateData(
                table: "Drivers",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                columns: new[] { "ContactInfo", "CreatedBy", "CreatedDate", "DriverID", "DriverName", "EmergencyContactJson", "HireDate", "LastPerformanceReview", "ModifiedDate", "PerformanceScore", "PersonalDetails", "RowVersion", "SalaryGrade", "Status" },
                values: new object[] { "", "", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, "", "", new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 5.0m, "", new byte[0], 0, "Active" });

            migrationBuilder.UpdateData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedDate", "FuelType", "IsMaintenanceRequired", "LastLocationUpdate", "LastMaintenanceDate", "Latitude", "Longitude", "MaintenanceDue", "MaintenanceHistoryJson", "MakeModel", "Mileage", "ModifiedDate", "NextMaintenanceDate", "RowVersion", "SpecificationsJson", "Status", "VehicleCode", "VehicleGuid", "VehicleId", "Year" },
                values: new object[] { new DateTime(2025, 5, 26, 12, 42, 55, 3, DateTimeKind.Local).AddTicks(4706), "", false, null, null, null, null, false, "", "", 0m, new DateTime(2025, 5, 26, 12, 42, 55, 3, DateTimeKind.Local).AddTicks(4793), null, null, "", "Available", null, new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), 0, null });

            migrationBuilder.UpdateData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedDate", "FuelType", "IsMaintenanceRequired", "LastLocationUpdate", "LastMaintenanceDate", "Latitude", "Longitude", "MaintenanceDue", "MaintenanceHistoryJson", "MakeModel", "Mileage", "ModifiedDate", "NextMaintenanceDate", "RowVersion", "SpecificationsJson", "Status", "VehicleCode", "VehicleGuid", "VehicleId", "Year" },
                values: new object[] { new DateTime(2025, 5, 26, 12, 42, 55, 3, DateTimeKind.Local).AddTicks(4802), "", false, null, null, null, null, false, "", "", 0m, new DateTime(2025, 5, 26, 12, 42, 55, 3, DateTimeKind.Local).AddTicks(4803), null, null, "", "Available", null, new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), 0, null });

            migrationBuilder.UpdateData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                columns: new[] { "CreatedDate", "FuelType", "IsMaintenanceRequired", "LastLocationUpdate", "LastMaintenanceDate", "Latitude", "Longitude", "MaintenanceDue", "MaintenanceHistoryJson", "MakeModel", "Mileage", "ModifiedDate", "NextMaintenanceDate", "RowVersion", "SpecificationsJson", "Status", "VehicleCode", "VehicleGuid", "VehicleId", "Year" },
                values: new object[] { new DateTime(2025, 5, 26, 12, 42, 55, 3, DateTimeKind.Local).AddTicks(4806), "", false, null, null, null, null, false, "", "", 0m, new DateTime(2025, 5, 26, 12, 42, 55, 3, DateTimeKind.Local).AddTicks(4807), null, null, "", "Available", null, new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), 0, null });

            migrationBuilder.UpdateData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                columns: new[] { "CreatedDate", "FuelType", "IsMaintenanceRequired", "LastLocationUpdate", "LastMaintenanceDate", "Latitude", "Longitude", "MaintenanceDue", "MaintenanceHistoryJson", "MakeModel", "Mileage", "ModifiedDate", "NextMaintenanceDate", "RowVersion", "SpecificationsJson", "Status", "VehicleCode", "VehicleGuid", "VehicleId", "Year" },
                values: new object[] { new DateTime(2025, 5, 26, 12, 42, 55, 3, DateTimeKind.Local).AddTicks(4809), "", false, null, null, null, null, false, "", "", 0m, new DateTime(2025, 5, 26, 12, 42, 55, 3, DateTimeKind.Local).AddTicks(4811), null, null, "", "Available", null, new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"), 0, null });

            migrationBuilder.UpdateData(
                table: "Vehicles",
                keyColumn: "Id",
                keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                columns: new[] { "CreatedDate", "FuelType", "IsMaintenanceRequired", "LastLocationUpdate", "LastMaintenanceDate", "Latitude", "Longitude", "MaintenanceDue", "MaintenanceHistoryJson", "MakeModel", "Mileage", "ModifiedDate", "NextMaintenanceDate", "RowVersion", "SpecificationsJson", "Status", "VehicleCode", "VehicleGuid", "VehicleId", "Year" },
                values: new object[] { new DateTime(2025, 5, 26, 12, 42, 55, 3, DateTimeKind.Local).AddTicks(4813), "", false, null, null, null, null, false, "", "", 0m, new DateTime(2025, 5, 26, 12, 42, 55, 3, DateTimeKind.Local).AddTicks(4814), null, null, "", "Available", null, new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), 0, null });
        }        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.DropTable(
                name: "CustomFields");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "FuelType",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "IsMaintenanceRequired",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "LastLocationUpdate",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "LastMaintenanceDate",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "MaintenanceDue",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "MaintenanceHistoryJson",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "MakeModel",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Mileage",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "NextMaintenanceDate",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "SpecificationsJson",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "VehicleCode",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "VehicleGuid",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "VehicleId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "Distance",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "RouteCode",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "RouteID",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "RouteName",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "ScheduleJson",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "StopsJson",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "ContactInfo",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "DriverID",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "DriverName",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "EmergencyContactJson",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "HireDate",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "LastPerformanceReview",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "PerformanceScore",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "PersonalDetails",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "SalaryGrade",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Drivers");
        }
    }
}
