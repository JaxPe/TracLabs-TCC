using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TracLabs.Application.Features.System.DTOs;
using TracLabs.Infrastructure.Persistence;

namespace TracLabs.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class StatusController : ControllerBase
{
    private readonly TracLabsDbContext _dbContext;

    public StatusController(TracLabsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Retorna o status de saúde e métricas em tempo real do banco de dados (versão, conexões e latências).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var updatedAt = DateTime.UtcNow;

        try
        {
            var connection = _dbContext.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            // 1. Versão do PostgreSQL
            string? version;
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SHOW server_version;";
                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                version = result?.ToString();
            }

            // 2. Max Connections
            int maxConnections = 0;
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SHOW max_connections;";
                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                if (result != null && int.TryParse(result.ToString(), out var max))
                {
                    maxConnections = max;
                }
            }

            // 3. Opened Connections para o banco atual
            int openedConnections = 0;
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT count(*)::int FROM pg_stat_activity WHERE datname = current_database();";
                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                if (result != null && int.TryParse(result.ToString(), out var opened))
                {
                    openedConnections = opened;
                }
            }

            // 4. Medição de Latência (3 queries sequenciais)
            double latency1 = await MeasureQueryLatencyMsAsync(connection, cancellationToken);
            double latency2 = await MeasureQueryLatencyMsAsync(connection, cancellationToken);
            double latency3 = await MeasureQueryLatencyMsAsync(connection, cancellationToken);

            var dbStatus = new DatabaseStatus(
                Status: "healthy",
                Version: version,
                MaxConnections: maxConnections,
                OpenedConnections: openedConnections,
                Latency: new DatabaseLatency(latency1, latency2, latency3));

            return Ok(new SystemStatusResponse(updatedAt, new DependenciesStatus(dbStatus)));
        }
        catch (Exception ex)
        {
            var unhealthyDb = new DatabaseStatus(
                Status: "unhealthy",
                Version: null,
                MaxConnections: 0,
                OpenedConnections: 0,
                Latency: new DatabaseLatency(null, null, null),
                Error: ex.Message);

            return StatusCode(503, new SystemStatusResponse(updatedAt, new DependenciesStatus(unhealthyDb)));
        }
    }

    private static async Task<double> MeasureQueryLatencyMsAsync(
        System.Data.Common.DbConnection connection,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT 1;";
            await cmd.ExecuteScalarAsync(cancellationToken);
        }
        sw.Stop();
        return Math.Round(sw.Elapsed.TotalMilliseconds, 3);
    }
}
