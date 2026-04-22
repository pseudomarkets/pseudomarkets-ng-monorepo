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
    [Migration("20260421030000_AddMarketHolidays")]
    public partial class AddMarketHolidays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "market_holidays",
                columns: table => new
                {
                    holiday_date = table.Column<DateOnly>(type: "date", nullable: false),
                    holiday_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_market_holidays", x => x.holiday_date);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO market_holidays (holiday_date, holiday_name)
                VALUES
                    (DATE '2026-01-01', 'New Year''s Day'),
                    (DATE '2026-01-19', 'Martin Luther King, Jr. Day'),
                    (DATE '2026-02-16', 'Washington''s Birthday'),
                    (DATE '2026-04-03', 'Good Friday'),
                    (DATE '2026-05-25', 'Memorial Day'),
                    (DATE '2026-06-19', 'Juneteenth National Independence Day'),
                    (DATE '2026-07-03', 'Independence Day observed'),
                    (DATE '2026-09-07', 'Labor Day'),
                    (DATE '2026-11-26', 'Thanksgiving Day'),
                    (DATE '2026-12-25', 'Christmas Day')
                ON CONFLICT (holiday_date) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "market_holidays");
        }
    }
}
