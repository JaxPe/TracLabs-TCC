using FluentAssertions;
using NSubstitute;
using TracLabs.Application.Features.Dashboard.Queries.GetDashboardMetrics;
using TracLabs.Application.Features.Dashboard.Queries.GetRecentActivity;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;
using Xunit;

namespace TracLabs.Application.Tests;

public class DashboardQueriesTests
{
    private readonly ICalibrationRepository _calibRepo = Substitute.For<ICalibrationRepository>();
    private readonly ICertificateRepository _certRepo = Substitute.For<ICertificateRepository>();
    private readonly IStandardEquipmentRepository _standardRepo = Substitute.For<IStandardEquipmentRepository>();

    [Fact]
    public async Task GetDashboardMetrics_Should_Aggregate_Counts_Accurately()
    {
        // Arrange
        _calibRepo.GetActiveCalibrationsCountAsync(Arg.Any<CancellationToken>())
            .Returns(7);
        _certRepo.GetIssuedCountSinceAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(45);
        _standardRepo.GetExpiringSoonAsync(30, Arg.Any<CancellationToken>())
            .Returns(new List<StandardEquipment>
            {
                new StandardEquipment("PAD-01", "M1", "Manuf", "SN1", "Balança", 0m, 0.001m, 2.0m, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(5), "CERT-01", "Lab Cal")
            });
        _calibRepo.GetNonConformingCountAsync(Arg.Any<CancellationToken>())
            .Returns(3);

        var handler = new GetDashboardMetricsQueryHandler(
            _calibRepo,
            _certRepo,
            _standardRepo);

        // Act
        var result = await handler.Handle(new GetDashboardMetricsQuery(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ActiveCalibrationsCount.Should().Be(7);
        result.IssuedCertificatesMonthCount.Should().Be(45);
        result.ExpiringStandardsCount.Should().Be(1);
        result.NonConformingCount.Should().Be(3);
    }

    [Fact]
    public async Task GetRecentActivity_Should_Format_Timeline_Events_Correctly()
    {
        // Arrange
        var calibRecord = new CalibrationRecord(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "usr-tecnico-1",
            "Carlos Eduardo Silva",
            DateTime.UtcNow,
            DateTime.UtcNow.AddYears(1),
            20.1m,
            55.0m);

        _calibRepo.GetRecentCalibrationsAsync(5, Arg.Any<CancellationToken>())
            .Returns(new List<CalibrationRecord> { calibRecord });

        var handler = new GetRecentActivityQueryHandler(_calibRepo);

        // Act
        var result = await handler.Handle(new GetRecentActivityQuery(5), CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        var ev = result[0];
        ev.OperatorName.Should().Be("Carlos Eduardo Silva");
        ev.Result.Should().Be(CalibrationResult.Pending);
        ev.Title.Should().StartWith("Calibração #");
    }
}
