using FluentAssertions;
using Microsoft.AspNetCore.Components;
using TracLabs.Frontend.Components.Pages;
using TracLabs.Frontend.Models;
using TracLabs.Frontend.Models.Enums;
using TracLabs.Frontend.Services;
using TracLabs.Frontend.Services.Auth;
using Xunit;

namespace TracLabs.Frontend.Tests;

/// <summary>
/// Unit and component validation tests for CalibracaoVolumetrica.razor:
/// - Validation error banner and error collection
/// - Pycnometer standard requirement when manual water density > 0
/// - Expired vs valid pycnometer standard detection
/// - Mandatory ISO 4787 / ISO 8655 standards enforcement
/// - Certificate generation blocking on invalid states
/// - Environmental condition propagation and point management
/// </summary>
public class CalibracaoVolumetricaUiValidationTests
{
    private readonly MockMetrologyDataService _dataService = new();
    private readonly UserContextService _userContext = new();
    private readonly TestNavigationManager _navManager = new();

    private async Task<CalibracaoVolumetrica> CreateInitializedComponentAsync(string? tag = null)
    {
        var component = new CalibracaoVolumetrica();
        ComponentTestHarness.InitializeComponent(component);
        ComponentTestHarness.SetProperty(component, "DataService", _dataService);
        ComponentTestHarness.SetProperty(component, "UserContext", _userContext);
        ComponentTestHarness.SetProperty(component, "Navigation", _navManager);

        if (!string.IsNullOrEmpty(tag))
        {
            ComponentTestHarness.SetProperty(component, "Tag", tag);
        }

        await ComponentTestHarness.InvokeAsync(component, "OnInitializedAsync");
        return component;
    }

    [Fact]
    public async Task DefaultInitialization_WithBenchmarkDataAndValidStandards_IsCompletelyValid()
    {
        var comp = await CreateInitializedComponentAsync();

        var isFormValid = ComponentTestHarness.GetProperty<bool>(comp, "IsFormValid");
        var errors = ComponentTestHarness.Invoke<List<string>>(comp, "GetValidationErrors")!;
        var cal = ComponentTestHarness.GetField<CalibrationRecordDto>(comp, "_calibration")!;

        isFormValid.Should().BeTrue();
        errors.Should().BeEmpty();
        cal.Measurements.Should().HaveCount(11);
        cal.MeanV20.Should().Be(99.9431m);
    }

    [Fact]
    public async Task InsufficientMeasurements_LessThan7Runs_ProducesValidationError_AndBlocksCertificate()
    {
        var comp = await CreateInitializedComponentAsync();
        var cal = ComponentTestHarness.GetField<CalibrationRecordDto>(comp, "_calibration")!;

        // Truncate to only 5 runs (< 7)
        cal.Measurements = cal.Measurements.Take(5).ToList();
        ComponentTestHarness.Invoke(comp, "Recalculate");

        var isFormValid = ComponentTestHarness.GetProperty<bool>(comp, "IsFormValid");
        var errors = ComponentTestHarness.Invoke<List<string>>(comp, "GetValidationErrors")!;

        isFormValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("Número insuficiente de repetições gravimétricas") && e.Contains("5 de 7"));

        // Attempting to generate certificate must be blocked
        await ComponentTestHarness.InvokeAsync(comp, "SaveAndGenerateCertificate");
        _navManager.LastNavigatedUri.Should().BeNull("Certificate generation must be blocked when form is invalid");
    }

    [Fact]
    public async Task MissingBalanceStandard_ProducesValidationError()
    {
        var comp = await CreateInitializedComponentAsync();
        var cal = ComponentTestHarness.GetField<CalibrationRecordDto>(comp, "_calibration")!;

        cal.BalanceStandardId = "";

        var isFormValid = ComponentTestHarness.GetProperty<bool>(comp, "IsFormValid");
        var errors = ComponentTestHarness.Invoke<List<string>>(comp, "GetValidationErrors")!;

        isFormValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("Balança analítica padrão não foi selecionada"));
    }

    [Fact]
    public async Task InvalidBalanceStandard_UnknownOrExpired_ProducesValidationError()
    {
        var comp = await CreateInitializedComponentAsync();
        var cal = ComponentTestHarness.GetField<CalibrationRecordDto>(comp, "_calibration")!;
        var standards = ComponentTestHarness.GetField<List<StandardEquipmentDto>>(comp, "_standards")!;

        // Expire the balance
        var balance = standards.First(s => s.Role == StandardRole.Balance);
        balance.CalibrationDueDate = DateTime.UtcNow.AddDays(-10);

        var isFormValid = ComponentTestHarness.GetProperty<bool>(comp, "IsFormValid");
        var errors = ComponentTestHarness.Invoke<List<string>>(comp, "GetValidationErrors")!;

        isFormValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("Balança analítica padrão") && e.Contains("inválida"));
    }

    [Fact]
    public async Task MissingLiquidThermometer_ProducesValidationError()
    {
        var comp = await CreateInitializedComponentAsync();
        var cal = ComponentTestHarness.GetField<CalibrationRecordDto>(comp, "_calibration")!;

        cal.LiquidThermometerStandardId = "";

        var isFormValid = ComponentTestHarness.GetProperty<bool>(comp, "IsFormValid");
        var errors = ComponentTestHarness.Invoke<List<string>>(comp, "GetValidationErrors")!;

        isFormValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("Termômetro de imersão (líquido) padrão não foi selecionado"));
    }

    [Fact]
    public async Task MissingBarometer_ProducesValidationError()
    {
        var comp = await CreateInitializedComponentAsync();
        var cal = ComponentTestHarness.GetField<CalibrationRecordDto>(comp, "_calibration")!;

        cal.BarometerStandardId = "";

        var isFormValid = ComponentTestHarness.GetProperty<bool>(comp, "IsFormValid");
        var errors = ComponentTestHarness.Invoke<List<string>>(comp, "GetValidationErrors")!;

        isFormValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("Barômetro digital padrão não foi selecionado"));
    }

    [Fact]
    public async Task IncompleteAmbientStandards_ProducesValidationError()
    {
        var comp = await CreateInitializedComponentAsync();
        var cal = ComponentTestHarness.GetField<CalibrationRecordDto>(comp, "_calibration")!;

        // Clear thermo-hygrometer AND separate ambient thermometer
        cal.ThermoHygrometerStandardId = "";
        cal.AmbientThermometerStandardId = "";
        cal.HygrometerStandardId = "pad-hig-02"; // Only hygrometer without thermometer

        var isFormValid = ComponentTestHarness.GetProperty<bool>(comp, "IsFormValid");
        var errors = ComponentTestHarness.Invoke<List<string>>(comp, "GetValidationErrors")!;

        isFormValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("Padrões ambientais incompletos"));
    }

    [Fact]
    public async Task SeparateAmbientStandards_BothAmbientThermometerAndHygrometer_IsValid()
    {
        var comp = await CreateInitializedComponentAsync();
        var cal = ComponentTestHarness.GetField<CalibrationRecordDto>(comp, "_calibration")!;

        cal.ThermoHygrometerStandardId = "";
        cal.AmbientThermometerStandardId = "pad-teramb-01";
        cal.HygrometerStandardId = "pad-hig-02";

        var isFormValid = ComponentTestHarness.GetProperty<bool>(comp, "IsFormValid");
        var errors = ComponentTestHarness.Invoke<List<string>>(comp, "GetValidationErrors")!;

        isFormValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ManualWaterDensity_WhenActiveWithoutPycnometer_BlocksCertificate()
    {
        var comp = await CreateInitializedComponentAsync();
        var cal = ComponentTestHarness.GetField<CalibrationRecordDto>(comp, "_calibration")!;

        // Enable manual density
        ComponentTestHarness.Invoke(comp, "OnToggleManualDensity", new ChangeEventArgs { Value = true });

        // Remove pycnometer selection
        cal.PycnometerStandardId = "";
        cal.WaterDensityGcm3 = 0.998205m;
        ComponentTestHarness.Invoke(comp, "Recalculate");

        var isFormValid = ComponentTestHarness.GetProperty<bool>(comp, "IsFormValid");
        var errors = ComponentTestHarness.Invoke<List<string>>(comp, "GetValidationErrors")!;

        isFormValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("Densidade manual da água informada") && e.Contains("obrigatório selecionar um Picnômetro"));

        // Guard clause inside SaveAndGenerateCertificate
        await ComponentTestHarness.InvokeAsync(comp, "SaveAndGenerateCertificate");
        _navManager.LastNavigatedUri.Should().BeNull();
    }

    [Fact]
    public async Task ManualWaterDensity_WithExpiredPycnometer_BlocksCertificate()
    {
        var comp = await CreateInitializedComponentAsync();
        var cal = ComponentTestHarness.GetField<CalibrationRecordDto>(comp, "_calibration")!;

        // Enable manual density
        ComponentTestHarness.Invoke(comp, "OnToggleManualDensity", new ChangeEventArgs { Value = true });

        // Select the expired pycnometer (PAD-PIC-02)
        cal.WaterDensityGcm3 = 0.998205m;
        cal.PycnometerStandardId = "pad-pic-02";
        ComponentTestHarness.Invoke(comp, "Recalculate");

        var hasValidPyc = ComponentTestHarness.GetProperty<bool>(comp, "HasValidPycnometerSelected");
        var isFormValid = ComponentTestHarness.GetProperty<bool>(comp, "IsFormValid");
        var errors = ComponentTestHarness.Invoke<List<string>>(comp, "GetValidationErrors")!;

        hasValidPyc.Should().BeFalse();
        isFormValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("Picnômetro padrão") && e.Contains("PAD-PIC-02") && e.Contains("inválido"));

        // Guard clause inside SaveAndGenerateCertificate
        await ComponentTestHarness.InvokeAsync(comp, "SaveAndGenerateCertificate");
        _navManager.LastNavigatedUri.Should().BeNull();
    }

    [Fact]
    public async Task ManualWaterDensity_WithValidPycnometer_PassesValidation_AndGeneratesCertificate()
    {
        var comp = await CreateInitializedComponentAsync();
        var cal = ComponentTestHarness.GetField<CalibrationRecordDto>(comp, "_calibration")!;

        // Enable manual density with valid pycnometer (pad-pic-01)
        ComponentTestHarness.Invoke(comp, "OnToggleManualDensity", new ChangeEventArgs { Value = true });
        cal.WaterDensityGcm3 = 0.998205m;
        cal.PycnometerStandardId = "pad-pic-01";
        ComponentTestHarness.Invoke(comp, "Recalculate");

        var hasValidPyc = ComponentTestHarness.GetProperty<bool>(comp, "HasValidPycnometerSelected");
        var isFormValid = ComponentTestHarness.GetProperty<bool>(comp, "IsFormValid");
        var errors = ComponentTestHarness.Invoke<List<string>>(comp, "GetValidationErrors")!;

        hasValidPyc.Should().BeTrue();
        isFormValid.Should().BeTrue();
        errors.Should().BeEmpty();

        // Successful certificate generation triggers navigation
        await ComponentTestHarness.InvokeAsync(comp, "SaveAndGenerateCertificate");
        _navManager.LastNavigatedUri.Should().NotBeNull();
        _navManager.LastNavigatedUri.Should().StartWith("/certificados/");
    }

    [Fact]
    public async Task AddMeasurementRow_InheritsCalibrationHeaderConditions()
    {
        var comp = await CreateInitializedComponentAsync();
        var cal = ComponentTestHarness.GetField<CalibrationRecordDto>(comp, "_calibration")!;

        cal.WaterTemperatureCelsius = 21.25m;
        cal.AmbientTemperatureCelsius = 22.10m;
        cal.AtmosphericPressureHpa = 1015.5m;
        cal.RelativeHumidityPercent = 58.0m;

        var countBefore = cal.Measurements.Count;
        ComponentTestHarness.Invoke(comp, "AddMeasurementRow");

        cal.Measurements.Should().HaveCount(countBefore + 1);
        var addedPoint = cal.Measurements.Last();
        addedPoint.RunNumber.Should().Be(countBefore + 1);
        addedPoint.WaterTemperatureCelsius.Should().Be(21.25m);
        addedPoint.AmbientTemperatureCelsius.Should().Be(22.10m);
        addedPoint.AtmosphericPressureHpa.Should().Be(1015.5m);
        addedPoint.RelativeHumidityPercent.Should().Be(58.0m);
    }

    [Fact]
    public async Task ApplyHeaderConditionsToAllRows_DistributesHeaderConditionsToAllPoints()
    {
        var comp = await CreateInitializedComponentAsync();
        var cal = ComponentTestHarness.GetField<CalibrationRecordDto>(comp, "_calibration")!;

        cal.WaterTemperatureCelsius = 19.85m;
        cal.AmbientTemperatureCelsius = 20.50m;
        cal.AtmosphericPressureHpa = 1012.0m;
        cal.RelativeHumidityPercent = 52.5m;

        ComponentTestHarness.Invoke(comp, "ApplyHeaderConditionsToAllRows");

        cal.Measurements.All(pt =>
            pt.WaterTemperatureCelsius == 19.85m &&
            pt.AmbientTemperatureCelsius == 20.50m &&
            pt.AtmosphericPressureHpa == 1012.0m &&
            pt.RelativeHumidityPercent == 52.5m
        ).Should().BeTrue();
    }

    [Fact]
    public async Task RemoveMeasurementRow_RemovesRowAndReindexesRunNumbers()
    {
        var comp = await CreateInitializedComponentAsync();
        var cal = ComponentTestHarness.GetField<CalibrationRecordDto>(comp, "_calibration")!;

        var countBefore = cal.Measurements.Count;
        ComponentTestHarness.Invoke(comp, "RemoveMeasurementRow", 2); // Remove 3rd point

        cal.Measurements.Should().HaveCount(countBefore - 1);
        for (int i = 0; i < cal.Measurements.Count; i++)
        {
            cal.Measurements[i].RunNumber.Should().Be(i + 1);
        }
    }

    [Fact]
    public async Task EquipmentChanged_UpdatesCalibrationTargetAndRecalculates()
    {
        var comp = await CreateInitializedComponentAsync();

        // Switch to PIC-CLI-002
        ComponentTestHarness.Invoke(comp, "OnEquipmentChanged", new ChangeEventArgs { Value = "PIC-CLI-002" });

        var selectedEq = ComponentTestHarness.GetField<ClientEquipmentDto>(comp, "_selectedEquipment")!;
        var cal = ComponentTestHarness.GetField<CalibrationRecordDto>(comp, "_calibration")!;

        selectedEq.LabTag.Should().Be("PIC-CLI-002");
        cal.EquipmentTag.Should().Be("PIC-CLI-002");
        cal.NominalCapacity.Should().Be(selectedEq.NominalCapacity);
        cal.MaxPermissibleError.Should().Be(selectedEq.MaxPermissibleError);
    }

    [Fact]
    public async Task EnvironmentalConditions_CalculatedAsAverageFromMeasurementPoints_AndFeedsTanakaDensity()
    {
        var comp = await CreateInitializedComponentAsync();
        var cal = ComponentTestHarness.GetField<CalibrationRecordDto>(comp, "_calibration")!;

        // Define point conditions with variation
        for (int i = 0; i < cal.Measurements.Count; i++)
        {
            cal.Measurements[i].WaterTemperatureCelsius = 20.00m + (i * 0.10m);
            cal.Measurements[i].AmbientTemperatureCelsius = 21.00m + (i * 0.10m);
            cal.Measurements[i].AtmosphericPressureHpa = 1013.0m + i;
            cal.Measurements[i].RelativeHumidityPercent = 50.0m + i;
        }

        ComponentTestHarness.Invoke(comp, "Recalculate");

        var expectedAvgWaterTemp = Math.Round(cal.Measurements.Average(p => p.WaterTemperatureCelsius), 2);
        cal.WaterTemperatureCelsius.Should().Be(expectedAvgWaterTemp);
        cal.CalculatedWaterDensity.Should().BeApproximately(MetrologyCalculations.CalculatePureWaterDensity(expectedAvgWaterTemp), 0.000001m);
    }

    [Fact]
    public async Task ReplicateFirstRowConditionsToAllRows_CopiesFirstPointEnvironmentalConditionsToAllRows()
    {
        var comp = await CreateInitializedComponentAsync();
        var cal = ComponentTestHarness.GetField<CalibrationRecordDto>(comp, "_calibration")!;

        cal.Measurements[0].WaterTemperatureCelsius = 21.50m;
        cal.Measurements[0].AmbientTemperatureCelsius = 22.30m;
        cal.Measurements[0].AtmosphericPressureHpa = 1018.0m;
        cal.Measurements[0].RelativeHumidityPercent = 45.0m;

        ComponentTestHarness.Invoke(comp, "ReplicateFirstRowConditionsToAllRows");

        cal.Measurements.All(p =>
            p.WaterTemperatureCelsius == 21.50m &&
            p.AmbientTemperatureCelsius == 22.30m &&
            p.AtmosphericPressureHpa == 1018.0m &&
            p.RelativeHumidityPercent == 45.0m
        ).Should().BeTrue();

        cal.WaterTemperatureCelsius.Should().Be(21.50m);
    }
}
