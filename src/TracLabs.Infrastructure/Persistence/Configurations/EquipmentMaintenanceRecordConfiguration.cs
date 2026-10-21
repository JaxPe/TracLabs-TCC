using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;

namespace TracLabs.Infrastructure.Persistence.Configurations;

public class EquipmentMaintenanceRecordConfiguration : IEntityTypeConfiguration<EquipmentMaintenanceRecord>
{
    public void Configure(EntityTypeBuilder<EquipmentMaintenanceRecord> builder)
    {
        builder.ToTable("equipment_maintenance_records");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(m => m.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(m => m.PerformedBy)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.ResultSummary)
            .HasMaxLength(500);

        // Chaves estrangeiras opcionais (pertence a Equipment ou StandardEquipment)
        builder.HasOne(m => m.Equipment)
            .WithMany(e => e.MaintenanceHistory)
            .HasForeignKey(m => m.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.StandardEquipment)
            .WithMany(s => s.MaintenanceHistory)
            .HasForeignKey(m => m.StandardEquipmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
