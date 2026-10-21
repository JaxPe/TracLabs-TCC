using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;

namespace TracLabs.Infrastructure.Persistence.Configurations;

public class EquipmentOccurrenceRecordConfiguration : IEntityTypeConfiguration<EquipmentOccurrenceRecord>
{
    public void Configure(EntityTypeBuilder<EquipmentOccurrenceRecord> builder)
    {
        builder.ToTable("equipment_occurrence_records");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Type)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(o => o.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(o => o.ActionTaken)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(o => o.ReportedBy)
            .IsRequired()
            .HasMaxLength(200);

        // Chaves estrangeiras opcionais (pertence a Equipment ou StandardEquipment)
        builder.HasOne(o => o.Equipment)
            .WithMany(e => e.OccurrenceHistory)
            .HasForeignKey(o => o.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.StandardEquipment)
            .WithMany(s => s.OccurrenceHistory)
            .HasForeignKey(o => o.StandardEquipmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
