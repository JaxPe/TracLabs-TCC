using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using TracLabs.Domain.Entities;
using TracLabs.Domain.ValueObjects;

namespace TracLabs.Infrastructure.Persistence.Configurations;

public class StandardCertificateConfiguration : IEntityTypeConfiguration<StandardCertificate>
{
    public void Configure(EntityTypeBuilder<StandardCertificate> builder)
    {
        builder.ToTable("standard_certificates");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CertificateNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.CalibratingLaboratory)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.CalibrationDate)
            .IsRequired();

        builder.Property(c => c.CalibrationDueDate)
            .IsRequired();

        builder.Property(c => c.CalibrationRangeMin)
            .HasPrecision(18, 6);

        builder.Property(c => c.CalibrationRangeMax)
            .HasPrecision(18, 6);

        builder.Property(c => c.IsActive)
            .IsRequired();

        builder.Property(c => c.FitType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(c => c.UncertaintyPolicy)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(c => c.Slope)
            .HasPrecision(18, 6);

        builder.Property(c => c.Intercept)
            .HasPrecision(18, 6);

        builder.Property(c => c.R2)
            .HasPrecision(18, 6);

        // Tabela de pontos RBC armazenada como jsonb indexável no PostgreSQL
        builder.Property(c => c.Points)
            .HasColumnType("jsonb")
            .HasConversion(
                points => JsonSerializer.Serialize(points, JsonSerializerOptions.Default),
                json => JsonSerializer.Deserialize<List<StandardCalibrationPoint>>(
                    json, JsonSerializerOptions.Default) ?? new List<StandardCalibrationPoint>()
            );

        // Relacionamento com StandardEquipment
        builder.HasOne(c => c.StandardEquipment)
            .WithMany(s => s.Certificates)
            .HasForeignKey(c => c.StandardEquipmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
