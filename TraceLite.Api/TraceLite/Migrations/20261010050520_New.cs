using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TraceLite.Migrations
{
    /// <inheritdoc />
    public partial class New : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HttpPerformanceRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TraceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    StatusCode = table.Column<int>(type: "integer", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HttpPerformanceRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TraceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StatusCode = table.Column<int>(type: "integer", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SqlPerformanceRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TraceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CommandType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CommandText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SqlPerformanceRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HttpPerformanceRecords_CreatedAtUtc",
                table: "HttpPerformanceRecords",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_HttpPerformanceRecords_TraceId",
                table: "HttpPerformanceRecords",
                column: "TraceId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceRecords_CreatedAtUtc",
                table: "PerformanceRecords",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceRecords_Path",
                table: "PerformanceRecords",
                column: "Path");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceRecords_TraceId",
                table: "PerformanceRecords",
                column: "TraceId");

            migrationBuilder.CreateIndex(
                name: "IX_SqlPerformanceRecords_CreatedAtUtc",
                table: "SqlPerformanceRecords",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SqlPerformanceRecords_TraceId",
                table: "SqlPerformanceRecords",
                column: "TraceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HttpPerformanceRecords");

            migrationBuilder.DropTable(
                name: "PerformanceRecords");

            migrationBuilder.DropTable(
                name: "SqlPerformanceRecords");
        }
    }
}
