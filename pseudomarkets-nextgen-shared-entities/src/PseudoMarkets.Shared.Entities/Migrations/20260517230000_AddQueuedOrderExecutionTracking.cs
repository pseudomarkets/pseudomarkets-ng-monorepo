using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PseudoMarkets.Shared.Entities.Database;

#nullable disable

namespace PseudoMarkets.Shared.Entities.Migrations
{
    [DbContext(typeof(PseudoMarketsDbContext))]
    [Migration("20260517230000_AddQueuedOrderExecutionTracking")]
    public partial class AddQueuedOrderExecutionTracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "failure_message",
                table: "queued_orders",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_attempted_at_utc",
                table: "queued_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "processed_at_utc",
                table: "queued_orders",
                type: "timestamp with time zone",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "failure_message",
                table: "queued_orders");

            migrationBuilder.DropColumn(
                name: "last_attempted_at_utc",
                table: "queued_orders");

            migrationBuilder.DropColumn(
                name: "processed_at_utc",
                table: "queued_orders");
        }
    }
}
