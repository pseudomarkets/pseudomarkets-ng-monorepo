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
    [Migration("20260421180000_AddTradingInstruments")]
    public partial class AddTradingInstruments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "trading_instruments",
                columns: table => new
                {
                    symbol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    trading_status = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    primary_instrument_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    secondary_instrument_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    closing_price = table.Column<double>(type: "double precision", nullable: false),
                    closing_price_date = table.Column<DateOnly>(type: "date", nullable: false),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trading_instruments", x => x.symbol);
                });

            migrationBuilder.CreateIndex(
                name: "IX_trading_instruments_secondary_instrument_type",
                table: "trading_instruments",
                column: "secondary_instrument_type");

            migrationBuilder.CreateIndex(
                name: "IX_trading_instruments_trading_status",
                table: "trading_instruments",
                column: "trading_status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "trading_instruments");
        }
    }
}
