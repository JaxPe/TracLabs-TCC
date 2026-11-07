using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;

namespace TracLabs.Infrastructure.Persistence.Configurations;

public class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        builder.ToTable("equipment");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(e => e.LabTag)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(e => e.LabTag)
            .IsUnique();

        builder.Property(e => e.Model)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Manufacturer)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.SerialNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.InstrumentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.SoftwareVersion)
            .HasMaxLength(50);

        builder.Property(e => e.FirmwareVersion)
            .HasMaxLength(50);

        builder.Property(e => e.CurrentLocation)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(e => e.AcceptanceCriteria)
            .HasMaxLength(1000);

        builder.Property(e => e.MaintenancePlan)
            .HasMaxLength(2000);

        builder.Property(e => e.InitialConditionNotes)
            .HasMaxLength(2000);

        builder.Property(e => e.Unit)
            .IsRequired()
            .HasMaxLength(20);

        // Decimais de medição — precisão alta para valores metrológicos
        builder.Property(e => e.RangeMin)
            .HasPrecision(18, 6);

        builder.Property(e => e.NominalCapacity)
            .HasPrecision(18, 6)
            .IsRequired(false);

        builder.Property(e => e.RangeMax)
            .HasPrecision(18, 6);

        builder.Property(e => e.CalibrationRangeMin)
            .HasPrecision(18, 6);

        builder.Property(e => e.CalibrationRangeMax)
            .HasPrecision(18, 6);

        builder.Property(e => e.Resolution)
            .HasPrecision(18, 6);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        // Relacionamento com Client
        builder.HasOne(e => e.Client)
            .WithMany(c => c.Equipment)
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relacionamento com CalibrationRecord
        builder.HasMany(e => e.CalibrationHistory)
            .WithOne(c => c.Equipment)
            .HasForeignKey(c => c.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relacionamento com MaintenanceHistory
        builder.HasMany(e => e.MaintenanceHistory)
            .WithOne(m => m.Equipment)
            .HasForeignKey(m => m.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relacionamento com OccurrenceHistory
        builder.HasMany(e => e.OccurrenceHistory)
            .WithOne(o => o.Equipment)
            .HasForeignKey(o => o.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
