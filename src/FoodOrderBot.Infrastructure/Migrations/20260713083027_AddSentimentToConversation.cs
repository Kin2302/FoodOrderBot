using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodOrderBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSentimentToConversation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NeedsAttention",
                table: "ConversationMessages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SentimentLabel",
                table: "ConversationMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SentimentScore",
                table: "ConversationMessages",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NeedsAttention",
                table: "ConversationMessages");

            migrationBuilder.DropColumn(
                name: "SentimentLabel",
                table: "ConversationMessages");

            migrationBuilder.DropColumn(
                name: "SentimentScore",
                table: "ConversationMessages");
        }
    }
}
