using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TracLabs.Domain.Entities;

namespace TracLabs.Infrastructure.Persistence.Configurations;

public class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> builder)
    {
        builder.ToTable("certificates");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CertificateNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(c => c.CertificateNumber)
            .IsUnique();

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(c => c.IssuedByUserId)
            .HasMaxLength(450);

        builder.Property(c => c.PdfFileName)
            .HasMaxLength(200);

        // PDF armazenado como bytea no PostgreSQL
        builder.Property(c => c.PdfContent)
            .HasColumnType("bytea");
    }
}