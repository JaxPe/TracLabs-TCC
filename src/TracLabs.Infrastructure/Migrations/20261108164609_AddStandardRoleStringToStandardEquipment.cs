using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TracLabs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStandardRoleStringToStandardEquipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "standard_equipment",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Balance");

            migrationBuilder.Sql("ALTER TABLE standard_equipment ALTER COLUMN \"Role\" DROP DEFAULT;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "standard_equipment");
        }
    }
}
