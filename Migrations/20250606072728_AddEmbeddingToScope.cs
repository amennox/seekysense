using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Migrations
{
    /// <inheritdoc />
    public partial class AddEmbeddingToScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Embedding",
                table: "Scopes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "Scopes");
        }
    }
}
