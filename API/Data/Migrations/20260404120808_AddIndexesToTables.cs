using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexesToTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Members_City_Country",
                table: "Members",
                columns: new[] { "City", "Country" });

            migrationBuilder.CreateIndex(
                name: "IX_Members_Gender",
                table: "Members",
                column: "Gender");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Members_City_Country",
                table: "Members");

            migrationBuilder.DropIndex(
                name: "IX_Members_Gender",
                table: "Members");
        }
    }
}
