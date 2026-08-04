using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodOrderBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFbOwnerUserIdToShop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FbOwnerUserId",
                table: "Shops",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FbOwnerUserId",
                table: "Shops");
        }
    }
}
