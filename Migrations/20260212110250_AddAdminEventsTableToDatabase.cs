using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AfghanPay.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminEventsTableToDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_event",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActoreAgentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActoreAdminId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CashoutRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric", nullable: true),
                    Fee = table.Column<decimal>(type: "numeric", nullable: true),
                    Commission = table.Column<decimal>(type: "numeric", nullable: true),
                    TxRef = table.Column<string>(type: "text", nullable: true),
                    SenderPhone = table.Column<string>(type: "text", nullable: true),
                    ReciverPhone = table.Column<string>(type: "text", nullable: true),
                    AgentCode = table.Column<string>(type: "text", nullable: true),
                    Staus = table.Column<string>(type: "text", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    DataJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_event", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_event");
        }
    }
}
