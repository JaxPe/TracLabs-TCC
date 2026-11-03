using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TracLabs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStandardCertificatesAndEquipmentRanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CalibrationRangeMax",
                table: "standard_equipment",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CalibrationRangeMin",
                table: "standard_equipment",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RangeMax",
                table: "standard_equipment",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RangeMin",
                table: "standard_equipment",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CalibrationRangeMax",
                table: "equipment",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CalibrationRangeMin",
                table: "equipment",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsExtrapolated",
                table: "calibration_standards_used",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "OperatingValue",
                table: "calibration_standards_used",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "StandardCertificateId",
                table: "calibration_standards_used",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarningMessage",
                table: "calibration_standards_used",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "standard_certificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StandardEquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CertificateNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CalibratingLaboratory = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CalibrationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CalibrationDueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CalibrationRangeMin = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    CalibrationRangeMax = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    FitType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UncertaintyPolicy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Slope = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    Intercept = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    R2 = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    Points = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_standard_certificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_standard_certificates_standard_equipment_StandardEquipmentId",
                        column: x => x.StandardEquipmentId,
                        principalTable: "standard_equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_calibration_standards_used_StandardCertificateId",
                table: "calibration_standards_used",
                column: "StandardCertificateId");

            migrationBuilder.CreateIndex(
                name: "IX_standard_certificates_StandardEquipmentId",
                table: "standard_certificates",
                column: "StandardEquipmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_calibration_standards_used_standard_certificates_StandardCe~",
                table: "calibration_standards_used",
                column: "StandardCertificateId",
                principalTable: "standard_certificates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_calibration_standards_used_standard_certificates_StandardCe~",
                table: "calibration_standards_used");

            migrationBuilder.DropTable(
                name: "standard_certificates");

            migrationBuilder.DropIndex(
                name: "IX_calibration_standards_used_StandardCertificateId",
                table: "calibration_standards_used");

            migrationBuilder.DropColumn(
                name: "CalibrationRangeMax",
                table: "standard_equipment");

            migrationBuilder.DropColumn(
                name: "CalibrationRangeMin",
                table: "standard_equipment");

            migrationBuilder.DropColumn(
                name: "RangeMax",
                table: "standard_equipment");

            migrationBuilder.DropColumn(
                name: "RangeMin",
                table: "standard_equipment");

            migrationBuilder.DropColumn(
                name: "CalibrationRangeMax",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "CalibrationRangeMin",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "IsExtrapolated",
                table: "calibration_standards_used");

            migrationBuilder.DropColumn(
                name: "OperatingValue",
                table: "calibration_standards_used");

            migrationBuilder.DropColumn(
                name: "StandardCertificateId",
                table: "calibration_standards_used");

            migrationBuilder.DropColumn(
                name: "WarningMessage",
                table: "calibration_standards_used");
        }
    }
}
