namespace TracLabs.Application.Features.System.DTOs;

public record SystemStatusResponse(
    DateTime UpdatedAt,
    DependenciesStatus Dependencies);

public record DependenciesStatus(
    DatabaseStatus Database);

public record DatabaseStatus(
    string Status,
    string? Version,
    int MaxConnections,
    int OpenedConnections,
    DatabaseLatency Latency,
    string? Error = null);

public record DatabaseLatency(
    double? FirstQuery,
    double? SecondQuery,
    double? ThirdQuery);
