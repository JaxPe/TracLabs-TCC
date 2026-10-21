using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;

namespace TracLabs.Infrastructure.Persistence.Configurations;

public class StandardEquipmentConfiguration : IEntityTypeConfiguration<StandardEquipment>
{
    public void Configure(EntityTypeBuilder<StandardEquipment> builder)
    {
        builder.ToTable("standard_equipment");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.LabTag)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(s => s.LabTag).IsUnique();

        builder.Property(s => s.Model).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Manufacturer).IsRequired().HasMaxLength(100);
        builder.Property(s => s.SerialNumber).IsRequired().HasMaxLength(100);
        builder.Property(s => s.InstrumentType).IsRequired().HasMaxLength(100);

        builder.Property(s => s.SoftwareVersion).HasMaxLength(50);
        builder.Property(s => s.FirmwareVersion).HasMaxLength(50);

        builder.Property(s => s.CurrentLocation)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.AcceptanceCriteria)
            .HasMaxLength(1000);

        builder.Property(s => s.CalibrationIntervalMonths)
            .IsRequired();

        builder.Property(s => s.ReferenceMaterialDocumentation)
            .HasMaxLength(2000);

        builder.Property(s => s.MaintenancePlan)
            .HasMaxLength(2000);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(s => s.Error).HasPrecision(18, 6);
        builder.Property(s => s.Correction).HasPrecision(18, 6);
        builder.Property(s => s.ExpandedUncertainty).HasPrecision(18, 6);
        builder.Property(s => s.CoverageFactor).HasPrecision(5, 2);
        builder.Property(s => s.Resolution).HasPrecision(18, 6);

        builder.Property(s => s.CalibrationCertificateNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.CalibratingLaboratory)
            .IsRequired()
            .HasMaxLength(200);

        // Relacionamento com MaintenanceHistory
        builder.HasMany(s => s.MaintenanceHistory)
            .WithOne(m => m.StandardEquipment)
            .HasForeignKey(m => m.StandardEquipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relacionamento com OccurrenceHistory
        builder.HasMany(s => s.OccurrenceHistory)
            .WithOne(o => o.StandardEquipment)
            .HasForeignKey(o => o.StandardEquipmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
