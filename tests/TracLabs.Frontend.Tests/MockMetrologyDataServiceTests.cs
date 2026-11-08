using System.Text;
using FluentAssertions;
using TracLabs.Frontend.Models;
using TracLabs.Frontend.Models.Enums;
using TracLabs.Frontend.Services;
using Xunit;

namespace TracLabs.Frontend.Tests;

public class MockMetrologyDataServiceTests
{
    private readonly MockMetrologyDataService _service = new();

    [Fact]
    public async Task GetCertificatePdfAsync_ByStringId_ReturnsValidPdfBytes()
    {
        var bytes = await _service.GetCertificatePdfAsync("CERT-2026-00042");

        bytes.Should().NotBeNull();
        bytes!.Length.Should().BeGreaterThan(300);

        // Header verification
        var header = Encoding.ASCII.GetString(bytes.Take(4).ToArray());
        header.Should().Be("%PDF");

        // Content verification
        var text = Encoding.Latin1.GetString(bytes);
        text.Should().Contain("TRACLABS - CERTIFICADO DE CALIBRACAO METROLOGICA");
        text.Should().Contain("CERT-2026-00042");
        text.Should().Contain("trailer");
        text.Should().Contain("%%EOF");
    }

    [Fact]
    public async Task GetCertificatePdfAsync_ByGuid_ReturnsValidPdfBytes()
    {
        var guid = Guid.NewGuid();
        var bytes = await _service.GetCertificatePdfAsync(guid);

        bytes.Should().NotBeNull();
        bytes!.Length.Should().BeGreaterThan(300);
        var header = Encoding.ASCII.GetString(bytes.Take(4).ToArray());
        header.Should().Be("%PDF");
    }

    [Theory]
    [InlineData(StandardRole.Balance)]
    [InlineData(StandardRole.LiquidThermometer)]
    [InlineData(StandardRole.AmbientThermometer)]
    [InlineData(StandardRole.Barometer)]
    [InlineData(StandardRole.Hygrometer)]
    [InlineData(StandardRole.ThermoHygrometer)]
    [InlineData(StandardRole.Pycnometer)]
    public async Task SeedStandards_CoversAllRequiredMetrologicalRoles(StandardRole role)
    {
        var standards = await _service.GetStandardEquipmentAsync(role: role);

        standards.Should().NotBeEmpty($"StandardRole.{role} must be seeded in the service");
        standards.Any(s => s.IsValid).Should().BeTrue($"At least one valid standard must exist for role {role}");
    }

    [Fact]
    public async Task SeedStandards_Pycnometer_HasValidRbcCertificate()
    {
        var pycnometers = await _service.GetStandardEquipmentAsync(role: StandardRole.Pycnometer);

        pycnometers.Should().HaveCountGreaterThanOrEqualTo(2);
        
        var validPyc = pycnometers.FirstOrDefault(p => p.IsValid);
        validPyc.Should().NotBeNull();
        validPyc!.CalibrationCertificateNumber.Should().NotBeNullOrWhiteSpace();
        validPyc.CalibratingLaboratory.Should().Contain("RBC");
        validPyc.DaysUntilExpiry.Should().BeGreaterThan(0);

        var expiredPyc = pycnometers.FirstOrDefault(p => !p.IsValid);
        expiredPyc.Should().NotBeNull();
        expiredPyc!.DaysUntilExpiry.Should().BeLessThan(0);
    }

    [Fact]
    public async Task GetStandardCertificatesAsync_ReturnsActiveAndHistoricalCertificates()
    {
        var certs = await _service.GetStandardCertificatesAsync(MockMetrologyDataService.StandardBalanceId);

        certs.Should().NotBeEmpty();
        certs.Should().HaveCountGreaterThanOrEqualTo(2);

        var active = certs.First();
        active.IsActive.Should().BeTrue();
        active.CertificateNumber.Should().Be("CAL-RBC-2025-8841");
        active.IsValid.Should().BeTrue();

        var historical = certs.Skip(1).First();
        historical.IsActive.Should().BeFalse();
        historical.Status.Should().Be("Substituído / Histórico");
    }

    [Fact]
    public async Task GetStandardCalibrationsUsedAsync_ReturnsChronologicalUsageHistory()
    {
        var usages = await _service.GetStandardCalibrationsUsedAsync(MockMetrologyDataService.StandardBalanceId);

        usages.Should().NotBeEmpty();
        usages.Should().HaveCountGreaterThanOrEqualTo(4);

        // Ordered descending by date
        usages.Should().BeInDescendingOrder(u => u.CalibrationDate);
        usages.All(u => u.RoleUsed == StandardRole.Balance).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateStandardAsync_UpdatesPropertiesAndEmitsTimelineEvent()
    {
        var standards = await _service.GetStandardEquipmentAsync(role: StandardRole.Barometer);
        var std = standards.First();
        var originalLocation = std.CurrentLocation;

        std.CurrentLocation = "Sala de Calibração 02 - Nova Bancada";
        await _service.UpdateStandardAsync(std);

        var updated = (await _service.GetStandardEquipmentAsync(role: StandardRole.Barometer)).First(s => s.Id == std.Id);
        updated.CurrentLocation.Should().Be("Sala de Calibração 02 - Nova Bancada");

        var events = await _service.GetRecentEventsAsync();
        events.Any(e => e.Details.Contains(std.LabTag) && e.Title.Contains("Atualizadas")).Should().BeTrue();
    }

    [Fact]
    public async Task SendStandardToCalibrationAsync_SetsInCalibrationAndEmitsEvent()
    {
        var standards = await _service.GetStandardEquipmentAsync(role: StandardRole.AmbientThermometer);
        var std = standards.First();

        await _service.SendStandardToCalibrationAsync(MockMetrologyDataService.StandardAmbientThermometerId, "IPT - Térmica", DateTime.UtcNow.AddDays(30));

        var updated = (await _service.GetStandardEquipmentAsync(role: StandardRole.AmbientThermometer)).First(s => s.Id == std.Id);
        updated.Status.Should().Be(StandardEquipmentStatus.InCalibration);
        updated.CalibratingLaboratory.Should().Be("IPT - Térmica");

        // Attempting to send again should throw InvalidOperationException
        var act = () => _service.SendStandardToCalibrationAsync(MockMetrologyDataService.StandardAmbientThermometerId, "IPT - Térmica", DateTime.UtcNow.AddDays(30));
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task RegisterStandardCertificateAsync_AddsNewActiveCertificateAndArchivesPrevious()
    {
        var newCert = new StandardCertificateDto
        {
            CertificateNumber = "CAL-RBC-2026-9999",
            CalibratingLaboratory = "IPT RBC CAL-001",
            CalibrationDate = DateTime.UtcNow,
            CalibrationDueDate = DateTime.UtcNow.AddYears(2),
            CalibrationRangeMin = 0.001m,
            CalibrationRangeMax = 220.0m,
            IsActive = true,
            ExpandedUncertainty = 0.00004m,
            CoverageFactor = 2.00m,
            Error = 0.00001m,
            Correction = -0.00001m
        };

        await _service.RegisterStandardCertificateAsync(MockMetrologyDataService.StandardBalanceId, newCert);

        var certs = await _service.GetStandardCertificatesAsync(MockMetrologyDataService.StandardBalanceId);
        certs.First().CertificateNumber.Should().Be("CAL-RBC-2026-9999");
        certs.First().IsActive.Should().BeTrue();

        // Old certificate should be deactivated
        certs.Skip(1).All(c => !c.IsActive).Should().BeTrue();

        var balance = (await _service.GetStandardEquipmentAsync(role: StandardRole.Balance)).First();
        balance.CalibrationCertificateNumber.Should().Be("CAL-RBC-2026-9999");
        balance.Status.Should().Be(StandardEquipmentStatus.Calibrated);
    }
}
