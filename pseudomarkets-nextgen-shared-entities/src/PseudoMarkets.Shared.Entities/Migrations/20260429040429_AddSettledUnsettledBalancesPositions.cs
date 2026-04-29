using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PseudoMarkets.Shared.Entities.Migrations
{
    /// <inheritdoc />
    public partial class AddSettledUnsettledBalancesPositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "settled_cost_basis_total",
                table: "positions",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "settled_quantity",
                table: "positions",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "unsettled_cost_basis_total",
                table: "positions",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "unsettled_quantity",
                table: "positions",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "settled_quantity_remaining",
                table: "position_lots",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "unsettled_quantity_remaining",
                table: "position_lots",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "settled_cash_balance",
                table: "account_balances",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "unsettled_cash_balance",
                table: "account_balances",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                """
                UPDATE account_balances
                SET settled_cash_balance = cash_balance,
                    unsettled_cash_balance = 0;
                """);

            migrationBuilder.Sql(
                """
                UPDATE positions
                SET settled_quantity = quantity,
                    unsettled_quantity = 0,
                    settled_cost_basis_total = cost_basis_total,
                    unsettled_cost_basis_total = 0;
                """);

            migrationBuilder.Sql(
                """
                UPDATE position_lots
                SET settled_quantity_remaining = quantity_remaining,
                    unsettled_quantity_remaining = 0;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_trade_executions_settlement_date_trade_side",
                table: "trade_executions",
                columns: new[] { "settlement_date", "trade_side" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_trade_executions_settlement_date_trade_side",
                table: "trade_executions");

            migrationBuilder.DropColumn(
                name: "settled_cost_basis_total",
                table: "positions");

            migrationBuilder.DropColumn(
                name: "settled_quantity",
                table: "positions");

            migrationBuilder.DropColumn(
                name: "unsettled_cost_basis_total",
                table: "positions");

            migrationBuilder.DropColumn(
                name: "unsettled_quantity",
                table: "positions");

            migrationBuilder.DropColumn(
                name: "settled_quantity_remaining",
                table: "position_lots");

            migrationBuilder.DropColumn(
                name: "unsettled_quantity_remaining",
                table: "position_lots");

            migrationBuilder.DropColumn(
                name: "settled_cash_balance",
                table: "account_balances");

            migrationBuilder.DropColumn(
                name: "unsettled_cash_balance",
                table: "account_balances");
        }
    }
}
