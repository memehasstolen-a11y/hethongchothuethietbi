using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hethongchothuethietbi.Migrations
{
    /// <inheritdoc />
    public partial class AddQuantityToRentalOrderDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "RentalOrderDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Equipments",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "RentalOrderDetails");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Equipments");
        }
    }
}
