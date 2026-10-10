using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraceLite.Migrations
{
    /// <inheritdoc />
    public partial class AddErrorDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "SqlPerformanceRecords",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorType",
                table: "SqlPerformanceRecords",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "HttpPerformanceRecords",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorType",
                table: "HttpPerformanceRecords",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "SqlPerformanceRecords");

            migrationBuilder.DropColumn(
                name: "ErrorType",
                table: "SqlPerformanceRecords");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "HttpPerformanceRecords");

            migrationBuilder.DropColumn(
                name: "ErrorType",
                table: "HttpPerformanceRecords");
        }
    }
}
