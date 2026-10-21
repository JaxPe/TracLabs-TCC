using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TracLabs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentIso17025AndStatusLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcceptanceCriteria",
                table: "standard_equipment",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CalibrationIntervalMonths",
                table: "standard_equipment",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CurrentLocation",
                table: "standard_equipment",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FirmwareVersion",
                table: "standard_equipment",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaintenancePlan",
                table: "standard_equipment",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "standard_equipment",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReferenceMaterialDocumentation",
                table: "standard_equipment",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoftwareVersion",
                table: "standard_equipment",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "standard_equipment",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AcceptanceCriteria",
                table: "equipment",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentLocation",
                table: "equipment",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FirmwareVersion",
                table: "equipment",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InitialConditionNotes",
                table: "equipment",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaintenancePlan",
                table: "equipment",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "equipment",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SoftwareVersion",
                table: "equipment",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "equipment_maintenance_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    StandardEquipmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaintenanceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    PerformedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ResultSummary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment_maintenance_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_equipment_maintenance_records_equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_equipment_maintenance_records_standard_equipment_StandardEq~",
                        column: x => x.StandardEquipmentId,
                        principalTable: "standard_equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "equipment_occurrence_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    StandardEquipmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurrenceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ActionTaken = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ReportedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ResolvedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment_occurrence_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_equipment_occurrence_records_equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_equipment_occurrence_records_standard_equipment_StandardEqu~",
                        column: x => x.StandardEquipmentId,
                        principalTable: "standard_equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_equipment_maintenance_records_EquipmentId",
                table: "equipment_maintenance_records",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_maintenance_records_StandardEquipmentId",
                table: "equipment_maintenance_records",
                column: "StandardEquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_occurrence_records_EquipmentId",
                table: "equipment_occurrence_records",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_equipment_occurrence_records_StandardEquipmentId",
                table: "equipment_occurrence_records",
                column: "StandardEquipmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "equipment_maintenance_records");

            migrationBuilder.DropTable(
                name: "equipment_occurrence_records");

            migrationBuilder.DropColumn(
                name: "AcceptanceCriteria",
                table: "standard_equipment");

            migrationBuilder.DropColumn(
                name: "CalibrationIntervalMonths",
                table: "standard_equipment");

            migrationBuilder.DropColumn(
                name: "CurrentLocation",
                table: "standard_equipment");

            migrationBuilder.DropColumn(
                name: "FirmwareVersion",
                table: "standard_equipment");

            migrationBuilder.DropColumn(
                name: "MaintenancePlan",
                table: "standard_equipment");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "standard_equipment");

            migrationBuilder.DropColumn(
                name: "ReferenceMaterialDocumentation",
                table: "standard_equipment");

            migrationBuilder.DropColumn(
                name: "SoftwareVersion",
                table: "standard_equipment");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "standard_equipment");

            migrationBuilder.DropColumn(
                name: "AcceptanceCriteria",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "CurrentLocation",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "FirmwareVersion",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "InitialConditionNotes",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "MaintenancePlan",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "SoftwareVersion",
                table: "equipment");
        }
    }
}
