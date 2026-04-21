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

            migrationBuilder.InsertData(
                table: "market_holidays",
                columns: new[] { "holiday_date", "holiday_name" },
                values: new object[,]
                {
                    { new DateOnly(2026, 1, 1), "New Year's Day" },
                    { new DateOnly(2026, 1, 19), "Martin Luther King, Jr. Day" },
                    { new DateOnly(2026, 2, 16), "Washington's Birthday" },
                    { new DateOnly(2026, 4, 3), "Good Friday" },
                    { new DateOnly(2026, 5, 25), "Memorial Day" },
                    { new DateOnly(2026, 6, 19), "Juneteenth National Independence Day" },
                    { new DateOnly(2026, 7, 3), "Independence Day observed" },
                    { new DateOnly(2026, 9, 7), "Labor Day" },
                    { new DateOnly(2026, 11, 26), "Thanksgiving Day" },
                    { new DateOnly(2026, 12, 25), "Christmas Day" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "market_holidays");
        }
    }
}
