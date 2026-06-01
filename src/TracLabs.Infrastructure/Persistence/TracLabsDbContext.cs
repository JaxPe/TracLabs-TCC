using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TracLabs.Domain.Entities;
using TracLabs.Infrastructure.Identity;
using TracLabs.Infrastructure.Persistence.Configurations;

namespace TracLabs.Infrastructure.Persistence;

public class TracLabsDbContext : IdentityDbContext<ApplicationUser>
{
    public TracLabsDbContext(DbContextOptions<TracLabsDbContext> options)
        : base(options) { }

    // ── DbSets ────────────────────────────────────────────────────────────────
    // Cada DbSet representa uma tabela no banco.

    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<StandardEquipment> StandardEquipment => Set<StandardEquipment>();
    public DbSet<CalibrationRecord> CalibrationRecords => Set<CalibrationRecord>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<Client> Clients => Set<Client>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // IMPORTANTE: chamar o base primeiro — o Identity precisa registrar
        // suas próprias tabelas (AspNetUsers, AspNetRoles, etc.) antes de nós.
        base.OnModelCreating(modelBuilder);

        // Aplica automaticamente todas as classes IEntityTypeConfiguration<T>
        // que existirem neste assembly — evita um OnModelCreating gigante.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TracLabsDbContext).Assembly);

        // Filtro global de soft delete — qualquer query feita via EF Core
        // automaticamente ignora registros com IsDeleted = true.
        // Isso vale para TODAS as entidades que herdam de BaseEntity.
        modelBuilder.Entity<Equipment>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<StandardEquipment>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<CalibrationRecord>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Certificate>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Client>().HasQueryFilter(e => !e.IsDeleted);
    }

    // ── Auditoria automática ──────────────────────────────────────────────────
    // Sobrescrever SaveChangesAsync nos permite interceptar TODA escrita no banco
    // e atualizar os campos de auditoria automaticamente, sem depender de
    // ninguém lembrar de chamar MarkAsUpdated() manualmente.

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    // CreatedAt já é setado no construtor de BaseEntity,
                    // mas garantimos aqui como segunda camada de segurança.
                    entry.Property(nameof(BaseEntity.CreatedAt)).CurrentValue
                        = DateTime.UtcNow;
                    break;

                case EntityState.Modified:
                    entry.Property(nameof(BaseEntity.UpdatedAt)).CurrentValue
                        = DateTime.UtcNow;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}