using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusBus.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverFirstLastNameAndVehicleBusNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "Drivers");

            migrationBuilder.RenameColumn(
                name: "RegistrationNumber",
                table: "Vehicles",
                newName: "BusNumber");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Drivers",
                newName: "LastName");

            migrationBuilder.RenameColumn(
                name: "LicenseNumber",
                table: "Drivers",
                newName: "FirstName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BusNumber",
                table: "Vehicles",
                newName: "RegistrationNumber");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "Drivers",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "Drivers",
                newName: "LicenseNumber");

            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "Vehicles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeId",
                table: "Drivers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
