using System.Text;
using FluentAssertions;
using TracLabs.Frontend.Models;
using TracLabs.Frontend.Models.Enums;
using TracLabs.Frontend.Services;
using Xunit;

namespace TracLabs.Frontend.Tests;

public class StandardsLifecycleAdversarialTests
{
    private readonly MockMetrologyDataService _service = new();

    [Fact]
    public async Task Pdf_Structure_ByteOffsets_And_Trailer_ExactValidation()
    {
        // Act
        var pdfBytes = await _service.GetCertificatePdfAsync("CERT-2026-00042");

        // Assert
        pdfBytes.Should().NotBeNull();
        var bytes = pdfBytes!;
        var text = Encoding.Latin1.GetString(bytes);

        // 1. Header validation
        text.Should().StartWith("%PDF-1.4\n");
        // Check binary marker bytes (greater than 127 to ensure 8-bit binary transport)
        bytes[9].Should().Be(0x25); // '%'
        bytes[10].Should().Be(0xE2);
        bytes[11].Should().Be(0xE3);
        bytes[12].Should().Be(0xCF);
        bytes[13].Should().Be(0xD3);

        // 2. Trailer & EOF validation
        text.TrimEnd('\r', '\n').Should().EndWith("%%EOF");
        text.Should().Contain("trailer\n<< /Size 7 /Root 1 0 R >>\nstartxref\n");

        // 3. Extract startxref offset and verify xref location
        var startXrefIdx = text.LastIndexOf("startxref\n", StringComparison.Ordinal);
        startXrefIdx.Should().BeGreaterThan(0);
        var eofIdx = text.LastIndexOf("%%EOF", StringComparison.Ordinal);
        var offsetStr = text.Substring(startXrefIdx + 10, eofIdx - (startXrefIdx + 10)).Trim();
        long.TryParse(offsetStr, out var xrefOffset).Should().BeTrue();

        // The byte at xrefOffset in the stream must be 'x' of 'xref'
        xrefOffset.Should().BeLessThan(bytes.Length);
        bytes[xrefOffset].Should().Be((byte)'x');
        bytes[xrefOffset + 1].Should().Be((byte)'r');
        bytes[xrefOffset + 2].Should().Be((byte)'e');
        bytes[xrefOffset + 3].Should().Be((byte)'f');

        // 4. Verify each object offset from the xref table
        var xrefLines = text.Substring((int)xrefOffset).Split('\n');
        xrefLines[0].Trim().Should().Be("xref");
        xrefLines[1].Trim().Should().Be("0 7");
        xrefLines[2].Trim().Should().Be("0000000000 65535 f");

        for (int objId = 1; objId <= 6; objId++)
        {
            var line = xrefLines[objId + 2];
            // PDF spec requires exactly 20 bytes per xref line including EOL
            // In Latin1 encoding, length of line + \n is 20
            var offset = long.Parse(line.Substring(0, 10));
            offset.Should().BeGreaterThan(0);
            offset.Should().BeLessThan(bytes.Length);

            // Object declaration must be found at exactly that offset
            var expectedObjHeader = $"{objId} 0 obj";
            var actualHeader = Encoding.Latin1.GetString(bytes.Skip((int)offset).Take(expectedObjHeader.Length).ToArray());
            actualHeader.Should().Be(expectedObjHeader, $"Object {objId} must start exactly at byte offset {offset}");
        }
    }

    [Fact]
    public async Task Pdf_Escapes_SpecialCharacters_WithoutCorruptingPdfSyntax()
    {
        // Arrange: Create a calibration with special characters that could break PDF syntax (parentheses, slashes)
        var calRecord = new CalibrationRecordDto
        {
            Id = Guid.NewGuid().ToString(),
            EquipmentTag = "TAG-(TEST)\\01",
            EquipmentDescription = "Balão Volumétrico (Especial) \\ Teste / Classe A",
            ClientName = "Química & Petróleo (Brasil) S.A. \\ Filial",
            CreatedAt = DateTime.UtcNow,
            NominalCapacity = 100.0m,
            MeanV20 = 99.9850m,
            SystematicError = -0.0150m,
            ExpandedUncertainty = 0.0075m,
            CoverageFactor = 2.00m,
            MaxPermissibleError = 0.100m,
            Result = CalibrationResult.Conforming,
            TechnicianName = "Técnico (Resp.)",
            BalanceStandardId = MockMetrologyDataService.StandardBalanceId.ToString(),
            LiquidThermometerStandardId = MockMetrologyDataService.StandardThermometerId.ToString(),
            BarometerStandardId = MockMetrologyDataService.StandardBarometerId.ToString()
        };

        var cert = await _service.CreateCertificateFromCalibrationAsync(calRecord);
        cert.Should().NotBeNull();

        // Act: Generate PDF for this specific certificate
        var pdfBytes = await _service.GetCertificatePdfAsync(cert.Id);

        // Assert
        pdfBytes.Should().NotBeNull();
        var text = Encoding.Latin1.GetString(pdfBytes!);

        // Special characters must be escaped: '(' -> '\(', ')' -> '\)', '\' -> '\\'
        text.Should().Contain(cert.CertificateNumber);
        text.Should().Contain(@"Química & Petróleo \(Brasil\) S.A. \\ Filial");
        text.Should().Contain(@"Balão Volumétrico \(Especial\) \\ Teste / Classe A");
        text.Should().Contain(@"TAG-\(TEST\)\\01");

        // Verify PDF integrity
        text.Should().StartWith("%PDF-1.4\n");
        text.TrimEnd('\r', '\n').Should().EndWith("%%EOF");
    }

    [Fact]
    public async Task Pdf_Retrieval_BySpecificId_And_FallbackBehavior()
    {
        // 1. By exact CertificateNumber
        var bytesByNum = await _service.GetCertificatePdfAsync("CERT-2026-00042");
        bytesByNum.Should().NotBeNull();
        Encoding.Latin1.GetString(bytesByNum!).Should().Contain("CERT-2026-00042");

        // 2. By non-existent ID in prototype mode: returns fallback seed certificate
        var bytesUnknown = await _service.GetCertificatePdfAsync("NON_EXISTENT_ID_9999");
        bytesUnknown.Should().NotBeNull("MockMetrologyDataService provides default seed certificate for UI preview fallback");
        Encoding.Latin1.GetString(bytesUnknown!).Should().Contain("TRACLABS - CERTIFICADO DE CALIBRACAO METROLOGICA");
    }

    [Fact]
    public async Task StandardsLifecycle_SendToCalibration_TransitionsState_And_EnforcesRules()
    {
        var standardId = MockMetrologyDataService.StandardThermometerId;
        var std = (await _service.GetStandardEquipmentAsync()).First(s => s.Id == "pad-term-01");
        std.Status = StandardEquipmentStatus.Calibrated; // Reset status

        // Act 1: Send to external calibration
        var expectedReturn = DateTime.UtcNow.AddDays(15);
        await _service.SendStandardToCalibrationAsync(standardId, "INMETRO DIMCI", expectedReturn);

        // Assert 1: Status must be InCalibration
        var updated = (await _service.GetStandardEquipmentAsync()).First(s => s.Id == "pad-term-01");
        updated.Status.Should().Be(StandardEquipmentStatus.InCalibration);
        updated.CalibratingLaboratory.Should().Be("INMETRO DIMCI");
        updated.IsValid.Should().BeFalse("Standard in calibration cannot be considered valid for operational use");

        // Assert 2: Double-sending must throw InvalidOperationException
        var actDoubleSend = () => _service.SendStandardToCalibrationAsync(standardId, "Another Lab", expectedReturn);
        await actDoubleSend.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*já está em processo de calibração*");

        // Assert 3: Empty laboratory must throw ArgumentException
        var actEmptyLab = () => _service.SendStandardToCalibrationAsync(MockMetrologyDataService.StandardBarometerId, "", expectedReturn);
        await actEmptyLab.Should().ThrowAsync<ArgumentException>();

        // Assert 4: Unknown standard ID must throw KeyNotFoundException
        var actUnknown = () => _service.SendStandardToCalibrationAsync(Guid.NewGuid(), "Lab Test", expectedReturn);
        await actUnknown.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task StandardsLifecycle_RegisterCertificate_RestoresCalibrated_Or_SetsPending_IfExpired()
    {
        var standardId = MockMetrologyDataService.StandardThermometerId;
        var std = (await _service.GetStandardEquipmentAsync()).First(s => s.Id == "pad-term-01");
        std.Status = StandardEquipmentStatus.InCalibration;

        // Act 1: Register valid new certificate
        var validCert = new StandardCertificateDto
        {
            CertificateNumber = "CAL-DIMCI-2026-0012",
            CalibratingLaboratory = "Inmetro DIMCI",
            CalibrationDate = DateTime.UtcNow,
            CalibrationDueDate = DateTime.UtcNow.AddYears(1),
            CalibrationRangeMin = -20m,
            CalibrationRangeMax = 100m,
            IsActive = true,
            ExpandedUncertainty = 0.02m,
            CoverageFactor = 2.00m,
            Error = 0.01m,
            Correction = -0.01m
        };

        await _service.RegisterStandardCertificateAsync(standardId, validCert);

        // Assert 1: Standard restored to Calibrated
        var calibratedStd = (await _service.GetStandardEquipmentAsync()).First(s => s.Id == "pad-term-01");
        calibratedStd.Status.Should().Be(StandardEquipmentStatus.Calibrated);
        calibratedStd.CalibrationCertificateNumber.Should().Be("CAL-DIMCI-2026-0012");
        calibratedStd.CalibratingLaboratory.Should().Be("Inmetro DIMCI");
        calibratedStd.CalibrationDueDate.Date.Should().Be(validCert.CalibrationDueDate.Date);
        calibratedStd.IsValid.Should().BeTrue();
        calibratedStd.Error.Should().Be(0.01m);
        calibratedStd.Correction.Should().Be(-0.01m);

        // Act 2: Register already EXPIRED certificate -> should set status to PendingCalibration, NOT Calibrated!
        calibratedStd.Status = StandardEquipmentStatus.InCalibration;
        var expiredCert = new StandardCertificateDto
        {
            CertificateNumber = "CAL-DIMCI-2023-OLD",
            CalibratingLaboratory = "Inmetro DIMCI",
            CalibrationDate = DateTime.UtcNow.AddYears(-2),
            CalibrationDueDate = DateTime.UtcNow.AddYears(-1), // Already expired
            IsActive = true
        };

        await _service.RegisterStandardCertificateAsync(standardId, expiredCert);

        var pendingStd = (await _service.GetStandardEquipmentAsync()).First(s => s.Id == "pad-term-01");
        pendingStd.Status.Should().Be(StandardEquipmentStatus.PendingCalibration);
        pendingStd.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task StandardsLifecycle_RegisterCertificate_ValidationRules()
    {
        var standardId = MockMetrologyDataService.StandardBarometerId;

        // 1. Null certificate
        var actNull = () => _service.RegisterStandardCertificateAsync(standardId, null!);
        await actNull.Should().ThrowAsync<ArgumentNullException>();

        // 2. Empty certificate number
        var emptyCertNum = new StandardCertificateDto { CertificateNumber = "", CalibratingLaboratory = "IPT" };
        var actEmptyNum = () => _service.RegisterStandardCertificateAsync(standardId, emptyCertNum);
        await actEmptyNum.Should().ThrowAsync<ArgumentException>().WithParameterName("CertificateNumber");

        // 3. Empty laboratory
        var emptyLab = new StandardCertificateDto { CertificateNumber = "CERT-001", CalibratingLaboratory = "" };
        var actEmptyLab = () => _service.RegisterStandardCertificateAsync(standardId, emptyLab);
        await actEmptyLab.Should().ThrowAsync<ArgumentException>().WithParameterName("CalibratingLaboratory");

        // 4. Due date before calibration date
        var invertedDates = new StandardCertificateDto
        {
            CertificateNumber = "CERT-001",
            CalibratingLaboratory = "IPT",
            CalibrationDate = DateTime.UtcNow,
            CalibrationDueDate = DateTime.UtcNow.AddDays(-1)
        };
        var actInvertedDates = () => _service.RegisterStandardCertificateAsync(standardId, invertedDates);
        await actInvertedDates.Should().ThrowAsync<ArgumentException>().WithParameterName("CalibrationDueDate");

        // 5. Unknown standard ID
        var actUnknownStd = () => _service.RegisterStandardCertificateAsync(Guid.NewGuid(), new StandardCertificateDto
        {
            CertificateNumber = "CERT-001",
            CalibratingLaboratory = "IPT",
            CalibrationDate = DateTime.UtcNow,
            CalibrationDueDate = DateTime.UtcNow.AddYears(1)
        });
        await actUnknownStd.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task StandardEquipment_Resolution_GuidVsString_And_CustomStandard()
    {
        // 1. By designated Guid
        var certsByGuid = await _service.GetStandardCertificatesAsync(MockMetrologyDataService.StandardBalanceId);
        certsByGuid.Should().NotBeEmpty();

        // 2. By custom standard added at runtime with dynamic Guid string ID
        var customGuid = Guid.NewGuid();
        var customStd = new StandardEquipmentDto
        {
            Id = customGuid.ToString(),
            LabTag = "PAD-CUST-01",
            Name = "Padrão Customizado Teste",
            Role = StandardRole.Balance,
            Resolution = 0.0001m,
            RangeMin = 0.001m,
            RangeMax = 220.0m,
            Status = StandardEquipmentStatus.Calibrated
        };
        await _service.SaveStandardEquipmentAsync(customStd);

        // Can send custom standard to calibration by parsed Guid
        await _service.SendStandardToCalibrationAsync(customGuid, "Lab Externo", DateTime.UtcNow.AddDays(10));
        var retrieved = (await _service.GetStandardEquipmentAsync()).First(s => s.Id == customGuid.ToString());
        retrieved.Status.Should().Be(StandardEquipmentStatus.InCalibration);

        // Can register certificate for custom standard
        var cert = new StandardCertificateDto
        {
            CertificateNumber = "CAL-CUST-9999",
            CalibratingLaboratory = "Lab Externo",
            CalibrationDate = DateTime.UtcNow,
            CalibrationDueDate = DateTime.UtcNow.AddYears(1),
            IsActive = true
        };
        await _service.RegisterStandardCertificateAsync(customGuid, cert);

        var retrievedAfter = (await _service.GetStandardEquipmentAsync()).First(s => s.Id == customGuid.ToString());
        retrievedAfter.Status.Should().Be(StandardEquipmentStatus.Calibrated);

        var customCerts = await _service.GetStandardCertificatesAsync(customGuid);
        customCerts.Should().ContainSingle(c => c.CertificateNumber == "CAL-CUST-9999");
    }

    [Fact]
    public async Task StandardCertificates_Ordering_And_Deactivation_Enforced()
    {
        var balanceId = MockMetrologyDataService.StandardBalanceId;
        var initialCerts = await _service.GetStandardCertificatesAsync(balanceId);

        initialCerts.Should().HaveCountGreaterThanOrEqualTo(2);
        initialCerts.First().IsActive.Should().BeTrue();
        initialCerts.Skip(1).All(c => !c.IsActive).Should().BeTrue();

        // Register a 3rd certificate
        var thirdCert = new StandardCertificateDto
        {
            CertificateNumber = "CAL-RBC-2027-NEW",
            CalibratingLaboratory = "IPT",
            CalibrationDate = DateTime.UtcNow,
            CalibrationDueDate = DateTime.UtcNow.AddYears(1),
            IsActive = true
        };
        await _service.RegisterStandardCertificateAsync(balanceId, thirdCert);

        var updatedCerts = await _service.GetStandardCertificatesAsync(balanceId);
        updatedCerts.First().CertificateNumber.Should().Be("CAL-RBC-2027-NEW");
        updatedCerts.First().IsActive.Should().BeTrue();

        // All older certificates must now have IsActive = false
        updatedCerts.Skip(1).All(c => !c.IsActive).Should().BeTrue();
    }
}
