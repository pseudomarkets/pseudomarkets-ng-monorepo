using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PseudoMarkets.Shared.Entities.Migrations
{
    public partial class AddOrderExecutions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "order_executions",
                columns: table => new
                {
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    execution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    symbol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    order_side = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    order_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    fill_price = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    gross_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    fees = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    net_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    posting_batch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    failure_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    failure_message = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    submitted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    executed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_executions", x => x.order_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_order_executions_execution_id",
                table: "order_executions",
                column: "execution_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_order_executions_status",
                table: "order_executions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_order_executions_symbol",
                table: "order_executions",
                column: "symbol");

            migrationBuilder.CreateIndex(
                name: "ix_order_executions_transaction_id",
                table: "order_executions",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_executions_user_id",
                table: "order_executions",
                column: "user_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "order_executions");
        }
    }
}
