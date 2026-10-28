using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;
using TracLabs.Domain.ValueObjects;

namespace TracLabs.Infrastructure.Persistence.Configurations;

public class CalibrationRecordConfiguration : IEntityTypeConfiguration<CalibrationRecord>
{
    public void Configure(EntityTypeBuilder<CalibrationRecord> builder)
    {
        builder.ToTable("calibration_records");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.TechnicianId)
            .IsRequired()
            .HasMaxLength(450); // Tamanho padrão do Id do IdentityUser

        builder.Property(c => c.TechnicianName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.TemperatureCelsius).HasPrecision(5, 2);
        builder.Property(c => c.HumidityPercent).HasPrecision(5, 2);
        builder.Property(c => c.MeanError).HasPrecision(18, 6);
        builder.Property(c => c.MaxAbsoluteError).HasPrecision(18, 6);
        builder.Property(c => c.CombinedStandardUncertainty).HasPrecision(18, 6);
        builder.Property(c => c.ExpandedUncertainty).HasPrecision(18, 6);
        builder.Property(c => c.CoverageFactor).HasPrecision(5, 2);

        builder.Property(c => c.Result)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(c => c.Observations).HasMaxLength(2000);

        // MeasurementPoints é uma lista de Value Objects serializada como JSON
        // numa única coluna — evita criar uma tabela filha para algo que
        // sempre é lido junto com o registro pai.
        builder.Property(c => c.MeasurementPoints)
            .HasColumnType("jsonb") // jsonb = JSON binário indexável no PostgreSQL
            .HasConversion(
                points => JsonSerializer.Serialize(points, JsonSerializerOptions.Default),
                json => JsonSerializer.Deserialize<List<MeasurementPoint>>(
                    json, JsonSerializerOptions.Default) ?? new List<MeasurementPoint>()
            );

        // Relacionamento com StandardEquipment
        builder.HasOne(c => c.StandardEquipment)
            .WithMany()
            .HasForeignKey(c => c.StandardEquipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relacionamento com CalibrationStandardUsed (Rastreabilidade ISO 17025)
        builder.HasMany(c => c.StandardsUsed)
            .WithOne(s => s.CalibrationRecord)
            .HasForeignKey(s => s.CalibrationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relacionamento 1:1 com Certificate
        builder.HasOne(c => c.Certificate)
            .WithOne(cert => cert.CalibrationRecord)
            .HasForeignKey<Certificate>(cert => cert.CalibrationRecordId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}