using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TracLabs.Domain.Entities;

namespace TracLabs.Infrastructure.Persistence.Configurations;

public class CertificateSequenceConfiguration : IEntityTypeConfiguration<CertificateSequence>
{
    public void Configure(EntityTypeBuilder<CertificateSequence> builder)
    {
        builder.ToTable("certificate_sequences");

        // Chave primária é o ano civil de referência
        builder.HasKey(s => s.Year);

        builder.Property(s => s.Year)
            .ValueGeneratedNever();

        builder.Property(s => s.LastNumber)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();
    }
}
