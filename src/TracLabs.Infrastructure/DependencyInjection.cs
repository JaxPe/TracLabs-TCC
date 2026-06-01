// src/TracLabs.Infrastructure/DependencyInjection.cs
// Reasoning: Infrastructure knows its own concrete implementations
// (TracLabsDbContext, EquipmentRepository, etc.) and registers them
// against the interfaces defined in Application/Domain.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TracLabs.Application.Interfaces;
using TracLabs.Infrastructure.Persistence;
using TracLabs.Infrastructure.Persistence.Repositories;

namespace TracLabs.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers all Infrastructure layer services into the DI container.
    /// Call this from Program.cs: builder.Services.AddInfrastructure(configuration);
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Database ─────────────────────────────────────────────────────────────
        services.AddDbContext<TracLabsDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                // This tells EF Core migrations live in Infrastructure, not API
                b => b.MigrationsAssembly(typeof(TracLabsDbContext).Assembly.FullName)
            )
        );

        // ── Repositories ──────────────────────────────────────────────────────────
        // Each interface (defined in Application) is mapped to its concrete
        // implementation (defined here in Infrastructure).
        // Scoped = one instance per HTTP request, which is correct for DB operations.
        services.AddScoped<IEquipmentRepository, EquipmentRepository>();
        services.AddScoped<IStandardEquipmentRepository, StandardEquipmentRepository>();
        services.AddScoped<ICalibrationRepository, CalibrationRepository>();
        services.AddScoped<ICertificateRepository, CertificateRepository>();

        // ── Unit of Work ──────────────────────────────────────────────────────────
        // Wraps the DbContext transaction — ensures multiple repository operations
        // succeed or fail together (atomicity).
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}