using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PseudoMarkets.Shared.Entities.Database;

#nullable disable

namespace PseudoMarkets.Shared.Entities.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(PseudoMarketsDbContext))]
    [Migration("20260421031500_AddTradeExecutionSettlementDates")]
    public partial class AddTradeExecutionSettlementDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "settlement_date",
                table: "trade_executions",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "trade_date",
                table: "trade_executions",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.Sql(
                """
                UPDATE trade_executions
                SET
                    trade_date = executed_at_utc::date,
                    settlement_date = executed_at_utc::date + INTERVAL '1 day'
                WHERE trade_date = DATE '0001-01-01'
                   OR settlement_date = DATE '0001-01-01';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_trade_executions_settlement_date",
                table: "trade_executions",
                column: "settlement_date");

            migrationBuilder.CreateIndex(
                name: "IX_trade_executions_trade_date",
                table: "trade_executions",
                column: "trade_date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_trade_executions_settlement_date",
                table: "trade_executions");

            migrationBuilder.DropIndex(
                name: "IX_trade_executions_trade_date",
                table: "trade_executions");

            migrationBuilder.DropColumn(
                name: "settlement_date",
                table: "trade_executions");

            migrationBuilder.DropColumn(
                name: "trade_date",
                table: "trade_executions");
        }
    }
}
