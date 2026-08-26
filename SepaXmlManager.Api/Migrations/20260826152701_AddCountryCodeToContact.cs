using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SepaXmlManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryCodeToContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Contacts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Contacts");
        }
    }
}
