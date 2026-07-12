using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventDrivenArchitecturePlayground.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AltEntityExpense : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Expenses",
                table: "Expenses");

            migrationBuilder.RenameTable(
                name: "Expenses",
                newName: "expenses");

            migrationBuilder.AddPrimaryKey(
                name: "PK_expenses",
                table: "expenses",
                column: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_expenses",
                table: "expenses");

            migrationBuilder.RenameTable(
                name: "expenses",
                newName: "Expenses");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Expenses",
                table: "Expenses",
                column: "id");
        }
    }
}
