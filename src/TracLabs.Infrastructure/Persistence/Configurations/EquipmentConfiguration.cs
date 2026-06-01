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

        builder.Property(e => e.LabTag)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(e => e.LabTag)
            .IsUnique();  // Dois equipamentos não podem ter o mesmo tag

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

        builder.Property(e => e.Unit)
            .IsRequired()
            .HasMaxLength(20);

        // Decimais de medição — precisão alta para valores metrológicos
        builder.Property(e => e.RangeMin)
            .HasPrecision(18, 6);

        builder.Property(e => e.RangeMax)
            .HasPrecision(18, 6);

        builder.Property(e => e.Resolution)
            .HasPrecision(18, 6);

        builder.Property(e => e.Status)
            .HasConversion<string>()  // Salva o enum como texto no banco (mais legível)
            .HasMaxLength(50);

        // Relacionamento com Client
        builder.HasOne(e => e.Client)
            .WithMany(c => c.Equipment)
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict); // Não deletar client em cascata

        // Relacionamento com CalibrationRecord
        builder.HasMany(e => e.CalibrationHistory)
            .WithOne(c => c.Equipment)
            .HasForeignKey(c => c.EquipmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}