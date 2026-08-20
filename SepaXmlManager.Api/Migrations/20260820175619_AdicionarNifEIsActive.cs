using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SepaXmlManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarNifEIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NIF",
                table: "Contacts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Companies",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NIF",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Companies");
        }
    }
}
