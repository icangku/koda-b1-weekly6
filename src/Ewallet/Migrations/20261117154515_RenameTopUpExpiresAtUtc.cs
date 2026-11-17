using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ewallet.Console.Migrations
{
    /// <inheritdoc />
    public partial class RenameTopUpExpiresAtUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExpiredAtUtc",
                table: "TopUpRequests",
                newName: "ExpiresAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExpiresAtUtc",
                table: "TopUpRequests",
                newName: "ExpiredAtUtc");
        }
    }
}
