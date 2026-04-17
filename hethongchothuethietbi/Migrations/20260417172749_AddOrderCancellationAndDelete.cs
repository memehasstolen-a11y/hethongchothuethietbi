using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hethongchothuethietbi.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCancellationAndDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "RentalOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "RentalOrders");
        }
    }
}
