using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AfghanPay.API.Migrations
{
    /// <inheritdoc />
    public partial class Addcashoutsys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cashout_agents_AgentId",
                table: "Cashout");

            migrationBuilder.DropForeignKey(
                name: "FK_Cashout_users_UserId",
                table: "Cashout");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Cashout",
                table: "Cashout");

            migrationBuilder.RenameTable(
                name: "Cashout",
                newName: "cashoutrequest");

            migrationBuilder.RenameIndex(
                name: "IX_Cashout_UserId",
                table: "cashoutrequest",
                newName: "IX_cashoutrequest_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Cashout_AgentId",
                table: "cashoutrequest",
                newName: "IX_cashoutrequest_AgentId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_cashoutrequest",
                table: "cashoutrequest",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_cashoutrequest_agents_AgentId",
                table: "cashoutrequest",
                column: "AgentId",
                principalTable: "agents",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_cashoutrequest_users_UserId",
                table: "cashoutrequest",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cashoutrequest_agents_AgentId",
                table: "cashoutrequest");

            migrationBuilder.DropForeignKey(
                name: "FK_cashoutrequest_users_UserId",
                table: "cashoutrequest");

            migrationBuilder.DropPrimaryKey(
                name: "PK_cashoutrequest",
                table: "cashoutrequest");

            migrationBuilder.RenameTable(
                name: "cashoutrequest",
                newName: "Cashout");

            migrationBuilder.RenameIndex(
                name: "IX_cashoutrequest_UserId",
                table: "Cashout",
                newName: "IX_Cashout_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_cashoutrequest_AgentId",
                table: "Cashout",
                newName: "IX_Cashout_AgentId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Cashout",
                table: "Cashout",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Cashout_agents_AgentId",
                table: "Cashout",
                column: "AgentId",
                principalTable: "agents",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Cashout_users_UserId",
                table: "Cashout",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
