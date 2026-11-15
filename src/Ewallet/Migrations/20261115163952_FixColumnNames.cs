using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ewallet.Console.Migrations
{
    /// <inheritdoc />
    public partial class FixColumnNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "Transactions",
                newName: "CompletedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CompletedAtUtc",
                table: "Transactions",
                newName: "UpdatedAtUtc");
        }
    }
}
