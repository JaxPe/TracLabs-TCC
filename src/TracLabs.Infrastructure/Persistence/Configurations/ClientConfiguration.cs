using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TracLabs.Domain.Entities;

namespace TracLabs.Infrastructure.Persistence.Configurations;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("clients");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.TaxId)
            .IsRequired()
            .HasMaxLength(14); // CNPJ tem 14 dígitos, CPF tem 11

        builder.HasIndex(c => c.TaxId)
            .IsUnique();

        builder.Property(c => c.TaxIdType)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(c => c.Email).HasMaxLength(200);
        builder.Property(c => c.Phone).HasMaxLength(20);
        builder.Property(c => c.ContactPerson).HasMaxLength(200);
        builder.Property(c => c.Street).HasMaxLength(300);
        builder.Property(c => c.City).HasMaxLength(100);
        builder.Property(c => c.State).HasMaxLength(2);
        builder.Property(c => c.PostalCode).HasMaxLength(8);
    }
}