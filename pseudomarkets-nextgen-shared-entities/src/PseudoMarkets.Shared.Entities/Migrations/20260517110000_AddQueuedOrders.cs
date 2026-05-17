using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PseudoMarkets.Shared.Entities.Database;

#nullable disable

namespace PseudoMarkets.Shared.Entities.Migrations
{
    [DbContext(typeof(PseudoMarketsDbContext))]
    [Migration("20260517110000_AddQueuedOrders")]
    public partial class AddQueuedOrders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "queued_orders",
                columns: table => new
                {
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    symbol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    order_side = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    order_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    queue_reason = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    submitted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_queued_orders", x => x.order_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_queued_orders_status",
                table: "queued_orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_queued_orders_submitted_at_utc",
                table: "queued_orders",
                column: "submitted_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_queued_orders_symbol",
                table: "queued_orders",
                column: "symbol");

            migrationBuilder.CreateIndex(
                name: "ix_queued_orders_user_id",
                table: "queued_orders",
                column: "user_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "queued_orders");
        }
    }
}
