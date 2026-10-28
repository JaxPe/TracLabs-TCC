using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TracLabs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultipleStandardsAndEnvironmentalPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "calibration_standards_used",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CalibrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StandardEquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LabTag = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InstrumentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CertificateNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CalibratingLaboratory = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Resolution = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    Correction = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    ExpandedUncertainty = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    CoverageFactor = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_calibration_standards_used", x => x.Id);
                    table.ForeignKey(
                        name: "FK_calibration_standards_used_calibration_records_CalibrationId",
                        column: x => x.CalibrationId,
                        principalTable: "calibration_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_calibration_standards_used_standard_equipment_StandardEquip~",
                        column: x => x.StandardEquipmentId,
                        principalTable: "standard_equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_calibration_standards_used_CalibrationId",
                table: "calibration_standards_used",
                column: "CalibrationId");

            migrationBuilder.CreateIndex(
                name: "IX_calibration_standards_used_StandardEquipmentId",
                table: "calibration_standards_used",
                column: "StandardEquipmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "calibration_standards_used");
        }
    }
}
