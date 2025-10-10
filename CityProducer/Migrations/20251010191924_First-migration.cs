using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CityProducer.Migrations
{
    /// <inheritdoc />
    public partial class Firstmigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alerts",
                columns: table => new
                {
                    alert_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    zone = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    window_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    window_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    evidence = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alerts", x => x.alert_id);
                });

            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    event_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    event_type = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    event_version = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    producer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    trace_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    partition_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ts_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    zone = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    geo_lat = table.Column<float>(type: "real", nullable: false),
                    geo_long = table.Column<float>(type: "real", nullable: false),
                    severity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_events", x => x.event_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alerts_created_at",
                table: "alerts",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_alerts_zone",
                table: "alerts",
                column: "zone");

            migrationBuilder.CreateIndex(
                name: "IX_events_event_type",
                table: "events",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "IX_events_partition_key",
                table: "events",
                column: "partition_key");

            migrationBuilder.CreateIndex(
                name: "IX_events_ts_utc",
                table: "events",
                column: "ts_utc");

            migrationBuilder.CreateIndex(
                name: "IX_events_zone",
                table: "events",
                column: "zone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alerts");

            migrationBuilder.DropTable(
                name: "events");
        }
    }
}
