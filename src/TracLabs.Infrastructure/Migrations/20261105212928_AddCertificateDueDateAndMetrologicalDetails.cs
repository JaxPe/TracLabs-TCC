using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TracLabs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificateDueDateAndMetrologicalDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CertificateDueDate",
                table: "calibration_standards_used",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AirDensityGcm3",
                table: "calibration_records",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EffectiveDegreesOfFreedom",
                table: "calibration_records",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WaterDensityGcm3",
                table: "calibration_records",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CertificateDueDate",
                table: "calibration_standards_used");

            migrationBuilder.DropColumn(
                name: "AirDensityGcm3",
                table: "calibration_records");

            migrationBuilder.DropColumn(
                name: "EffectiveDegreesOfFreedom",
                table: "calibration_records");

            migrationBuilder.DropColumn(
                name: "WaterDensityGcm3",
                table: "calibration_records");
        }
    }
}
