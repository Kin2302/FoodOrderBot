using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodOrderBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Orders",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Note",
                table: "Orders");
        }
    }
}
