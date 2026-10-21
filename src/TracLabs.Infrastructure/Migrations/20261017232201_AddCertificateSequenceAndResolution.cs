using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TracLabs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificateSequenceAndResolution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Resolution",
                table: "standard_equipment",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "certificate_sequences",
                columns: table => new
                {
                    Year = table.Column<int>(type: "integer", nullable: false),
                    LastNumber = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_certificate_sequences", x => x.Year);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "certificate_sequences");

            migrationBuilder.DropColumn(
                name: "Resolution",
                table: "standard_equipment");
        }
    }
}
