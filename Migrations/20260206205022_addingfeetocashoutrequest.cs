using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AfghanPay.API.Migrations
{
    /// <inheritdoc />
    public partial class addingfeetocashoutrequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "fee",
                table: "cashoutrequest",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fee",
                table: "cashoutrequest");
        }
    }
}
