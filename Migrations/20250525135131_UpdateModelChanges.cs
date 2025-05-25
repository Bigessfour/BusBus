using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusBus.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModelChanges : Migration
    {        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);
            
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);
            
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Vehicles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
