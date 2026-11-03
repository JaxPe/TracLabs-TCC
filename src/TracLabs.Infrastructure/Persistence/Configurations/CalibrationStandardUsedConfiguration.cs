using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TracLabs.Domain.Entities;

namespace TracLabs.Infrastructure.Persistence.Configurations;

public class CalibrationStandardUsedConfiguration : IEntityTypeConfiguration<CalibrationStandardUsed>
{
    public void Configure(EntityTypeBuilder<CalibrationStandardUsed> builder)
    {
        builder.ToTable("calibration_standards_used");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Role)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.LabTag)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.Model)
            .HasMaxLength(100);

        builder.Property(s => s.Manufacturer)
            .HasMaxLength(100);

        builder.Property(s => s.SerialNumber)
            .HasMaxLength(100);

        builder.Property(s => s.InstrumentType)
            .HasMaxLength(100);

        builder.Property(s => s.CertificateNumber)
            .HasMaxLength(100);

        builder.Property(s => s.CalibratingLaboratory)
            .HasMaxLength(150);

        builder.Property(s => s.Resolution).HasPrecision(18, 6);
        builder.Property(s => s.Correction).HasPrecision(18, 6);
        builder.Property(s => s.ExpandedUncertainty).HasPrecision(18, 6);
        builder.Property(s => s.CoverageFactor).HasPrecision(5, 2);

        builder.Property(s => s.OperatingValue).HasPrecision(18, 6);
        builder.Property(s => s.IsExtrapolated).IsRequired();
        builder.Property(s => s.WarningMessage).HasMaxLength(500);

        builder.HasOne(s => s.CalibrationRecord)
            .WithMany(c => c.StandardsUsed)
            .HasForeignKey(s => s.CalibrationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.StandardEquipment)
            .WithMany()
            .HasForeignKey(s => s.StandardEquipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.StandardCertificate)
            .WithMany()
            .HasForeignKey(s => s.StandardCertificateId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
