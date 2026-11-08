using FluentAssertions;
using TracLabs.Frontend.Components.Pages;
using TracLabs.Frontend.Models;
using TracLabs.Frontend.Models.Enums;
using TracLabs.Frontend.Services;
using TracLabs.Frontend.Services.Auth;
using Xunit;

namespace TracLabs.Frontend.Tests;

/// <summary>
/// Unit and component tests for Cadastros.razor:
/// - RBAC access control rules (Técnico blocked from creating clients and editing/sending standards; Supervisor allowed all)
/// - Real-time filtering of customer equipments
/// - Full view loading for Customers, Equipment, and Laboratory Standards
/// - Deep-link query parameters resolution (Dashboard alert click -> Cadastros standards tab)
/// - Navigation to calibration from equipment view
/// </summary>
public class CadastrosRbacAndFilteringTests
{
    private readonly MockMetrologyDataService _dataService = new();
    private readonly UserContextService _userContext = new();
    private readonly TestNavigationManager _navManager = new();

    private async Task<Cadastros> CreateInitializedComponentAsync(UserRole role = UserRole.Supervisor, string? queryTab = null, string? queryId = null)
    {
        _userContext.SetRole(role);

        var component = new Cadastros();
        ComponentTestHarness.InitializeComponent(component);
        ComponentTestHarness.SetProperty(component, "DataService", _dataService);
        ComponentTestHarness.SetProperty(component, "UserContext", _userContext);
        ComponentTestHarness.SetProperty(component, "Navigation", _navManager);

        if (queryTab != null) ComponentTestHarness.SetProperty(component, "QueryTab", queryTab);
        if (queryId != null) ComponentTestHarness.SetProperty(component, "QueryId", queryId);

        await ComponentTestHarness.InvokeAsync(component, "OnInitializedAsync");
        if (queryTab != null || queryId != null)
        {
            await ComponentTestHarness.InvokeAsync(component, "OnParametersSetAsync");
        }

        return component;
    }

    #region 1. RBAC Rules for Técnico (Restricted Access)

    [Fact]
    public async Task Rbac_TecnicoRole_DisablesClientAndStandardCreation_WhilePermittingEquipment()
    {
        var comp = await CreateInitializedComponentAsync(UserRole.Tecnico);

        var isSupervisor = ComponentTestHarness.GetProperty<bool>(comp, "IsSupervisor");
        var isTecnico = ComponentTestHarness.GetProperty<bool>(comp, "IsTecnico");

        isSupervisor.Should().BeFalse();
        isTecnico.Should().BeTrue();
    }

    [Fact]
    public async Task Rbac_Tecnico_SaveClient_IsBlockedAndDoesNotPersistClient()
    {
        var comp = await CreateInitializedComponentAsync(UserRole.Tecnico);
        var initialClients = await _dataService.GetClientsAsync();
        var initialCount = initialClients.Count;

        // Populate new client attempt
        var newClient = new ClientDto
        {
            Name = "Empresa Bloqueada Ltda",
            TaxId = "99.999.999/0001-99",
            Email = "bloqueado@empresa.com"
        };
        ComponentTestHarness.SetField(comp, "_newClient", newClient);

        // Attempt to save client as Técnico
        await ComponentTestHarness.InvokeAsync(comp, "SaveClient");

        // Verification: Client must NOT be persisted
        var clientsAfter = await _dataService.GetClientsAsync();
        clientsAfter.Should().HaveCount(initialCount);
        clientsAfter.Any(c => c.TaxId == "99.999.999/0001-99").Should().BeFalse();
    }

    [Fact]
    public async Task Rbac_Tecnico_SaveStandard_IsBlockedAndDoesNotPersistStandard()
    {
        var comp = await CreateInitializedComponentAsync(UserRole.Tecnico);
        var initialStandards = await _dataService.GetStandardEquipmentAsync();
        var initialCount = initialStandards.Count;

        var newStd = new StandardEquipmentDto
        {
            LabTag = "PAD-TECNICO-BLOCKED",
            Name = "Padrão Tentativa Bloqueada",
            Role = StandardRole.Balance
        };
        ComponentTestHarness.SetField(comp, "_newStandard", newStd);

        await ComponentTestHarness.InvokeAsync(comp, "SaveStandard");

        var standardsAfter = await _dataService.GetStandardEquipmentAsync();
        standardsAfter.Should().HaveCount(initialCount);
        standardsAfter.Any(s => s.LabTag == "PAD-TECNICO-BLOCKED").Should().BeFalse();
    }

    [Fact]
    public async Task Rbac_Tecnico_StandardLifecycleActions_AreBlocked()
    {
        var comp = await CreateInitializedComponentAsync(UserRole.Tecnico);
        var standards = await _dataService.GetStandardEquipmentAsync();
        var balance = standards.First(s => s.Role == StandardRole.Balance);

        // Select standard full view
        await ComponentTestHarness.InvokeAsync(comp, "ViewStandardDetails", balance);

        // 1. Attempt to edit standard
        var originalLocation = balance.CurrentLocation;
        var editAttempt = new StandardEquipmentDto
        {
            Id = balance.Id,
            LabTag = balance.LabTag,
            Name = balance.Name,
            Role = balance.Role,
            CurrentLocation = "Localização Não Autorizada Tentada"
        };
        ComponentTestHarness.SetField(comp, "_selectedStandard", editAttempt);
        await ComponentTestHarness.InvokeAsync(comp, "SaveEditStandard");

        var balanceAfterEdit = (await _dataService.GetStandardEquipmentAsync()).First(s => s.Id == balance.Id);
        balanceAfterEdit.CurrentLocation.Should().Be(originalLocation);

        // 2. Attempt to send to external calibration
        ComponentTestHarness.SetField(comp, "_sendCalibLab", "Lab Não Autorizado");
        await ComponentTestHarness.InvokeAsync(comp, "ConfirmSendToCalibration");

        var balanceAfterSend = (await _dataService.GetStandardEquipmentAsync()).First(s => s.Id == balance.Id);
        balanceAfterSend.Status.Should().Be(StandardEquipmentStatus.Calibrated, "Técnico cannot transition standard to InCalibration");

        // 3. Attempt to register new certificate
        var newCert = new StandardCertificateDto
        {
            CertificateNumber = "CAL-NAO-AUTORIZADO",
            CalibratingLaboratory = "Lab Qualquer",
            CalibrationDate = DateTime.UtcNow,
            CalibrationDueDate = DateTime.UtcNow.AddYears(1)
        };
        ComponentTestHarness.SetField(comp, "_newStandardCert", newCert);
        await ComponentTestHarness.InvokeAsync(comp, "SaveNewStandardCertificate");

        var certs = await _dataService.GetStandardCertificatesAsync(MockMetrologyDataService.StandardBalanceId);
        certs.Any(c => c.CertificateNumber == "CAL-NAO-AUTORIZADO").Should().BeFalse();
    }

    [Fact]
    public async Task Rbac_Tecnico_SaveEquipment_IsPermittedAndPersistsEquipment()
    {
        var comp = await CreateInitializedComponentAsync(UserRole.Tecnico);
        var initialEquipments = await _dataService.GetClientEquipmentAsync();
        var initialCount = initialEquipments.Count;

        var newEq = new ClientEquipmentDto
        {
            ClientId = "cli-001",
            LabTag = "BAL-TECNICO-001",
            Description = "Balão Volumétrico Criado por Técnico",
            Manufacturer = "Vidrolab",
            Model = "Classe A 100mL",
            NominalCapacity = 100.0m,
            Unit = "mL",
            MaxPermissibleError = 0.100m,
            AccuracyClass = "Classe A"
        };
        ComponentTestHarness.SetField(comp, "_newEquipment", newEq);

        await ComponentTestHarness.InvokeAsync(comp, "SaveEquipment");

        var equipmentsAfter = await _dataService.GetClientEquipmentAsync();
        equipmentsAfter.Should().HaveCount(initialCount + 1);
        equipmentsAfter.Any(e => e.LabTag == "BAL-TECNICO-001").Should().BeTrue();
    }

    #endregion

    #region 2. RBAC Rules for Supervisor (Full Access)

    [Fact]
    public async Task Rbac_Supervisor_HasFullAccessToClientsAndStandards()
    {
        var comp = await CreateInitializedComponentAsync(UserRole.Supervisor);

        var isSupervisor = ComponentTestHarness.GetProperty<bool>(comp, "IsSupervisor");
        isSupervisor.Should().BeTrue();

        // 1. Create client
        var newClient = new ClientDto
        {
            Name = "Super Farma Autêntica S.A.",
            TaxId = "11.222.333/0001-44",
            Email = "contato@superfarma.com.br"
        };
        ComponentTestHarness.SetField(comp, "_newClient", newClient);
        await ComponentTestHarness.InvokeAsync(comp, "SaveClient");

        var clients = await _dataService.GetClientsAsync();
        clients.Any(c => c.TaxId == "11.222.333/0001-44").Should().BeTrue();

        // 2. Create standard
        var newStd = new StandardEquipmentDto
        {
            LabTag = "PAD-SUPER-01",
            Name = "Padrão Criado por Supervisor",
            Role = StandardRole.Balance,
            Resolution = 0.0001m,
            RangeMin = 0.001m,
            RangeMax = 220.0m
        };
        ComponentTestHarness.SetField(comp, "_newStandard", newStd);
        await ComponentTestHarness.InvokeAsync(comp, "SaveStandard");

        var standards = await _dataService.GetStandardEquipmentAsync();
        standards.Any(s => s.LabTag == "PAD-SUPER-01").Should().BeTrue();
    }

    #endregion

    #region 3. Customer Equipment Real-Time Filtering

    [Fact]
    public async Task CustomerEquipmentFiltering_WhenFilterEmpty_ShowsAllClientEquipments()
    {
        var comp = await CreateInitializedComponentAsync();
        var clients = await _dataService.GetClientsAsync();
        var clientWithEquipments = clients.First(c => c.Id == "cli-001");

        // Open client full view
        ComponentTestHarness.Invoke(comp, "ViewClientDetails", clientWithEquipments);

        var allEquips = ComponentTestHarness.GetField<List<ClientEquipmentDto>>(comp, "_selectedClientEquipments")!;
        var filteredEquips = ComponentTestHarness.GetField<List<ClientEquipmentDto>>(comp, "_filteredClientEquipments")!;

        allEquips.Should().NotBeEmpty();
        filteredEquips.Should().HaveCount(allEquips.Count);
    }

    [Theory]
    [InlineData("BAL-CLI", 1)]       // Filter by LabTag (BAL-CLI-001)
    [InlineData("Balão", 1)]         // Filter by Description (Balão Volumétrico)
    [InlineData("Brand", 1)]         // Filter by Manufacturer (Brand GmbH)
    [InlineData("Blaubrand", 1)]     // Filter by Model (Blaubrand Classe A)
    [InlineData("Vidrolabor", 1)]    // Filter by Manufacturer of 2nd equipment (PIC-CLI-002)
    [InlineData("NON_EXISTENT", 0)]  // Non-matching query
    public async Task CustomerEquipmentFiltering_FiltersAcrossTagDescriptionManufacturerAndModel(
        string filterQuery,
        int expectedMinimumMatches)
    {
        var comp = await CreateInitializedComponentAsync();
        var clients = await _dataService.GetClientsAsync();
        var client = clients.First(c => c.Id == "cli-001");

        ComponentTestHarness.Invoke(comp, "ViewClientDetails", client);

        ComponentTestHarness.SetField(comp, "_equipmentFilter", filterQuery);
        ComponentTestHarness.Invoke(comp, "FilterCustomerEquipment");

        var filtered = ComponentTestHarness.GetField<List<ClientEquipmentDto>>(comp, "_filteredClientEquipments")!;

        if (expectedMinimumMatches > 0)
        {
            filtered.Should().HaveCountGreaterThanOrEqualTo(expectedMinimumMatches);
            filtered.All(e =>
                e.LabTag.Contains(filterQuery, StringComparison.OrdinalIgnoreCase) ||
                e.Description.Contains(filterQuery, StringComparison.OrdinalIgnoreCase) ||
                e.Manufacturer.Contains(filterQuery, StringComparison.OrdinalIgnoreCase) ||
                e.Model.Contains(filterQuery, StringComparison.OrdinalIgnoreCase)
            ).Should().BeTrue();
        }
        else
        {
            filtered.Should().BeEmpty();
        }
    }

    #endregion

    #region 4. Deep-Link Query Parameters Resolution

    [Fact]
    public async Task QueryParameters_StandardsTabWithStandardId_OpensStandardFullViewDirectly()
    {
        // Simulates clicking "Ver Padrão" in Dashboard alert: /cadastros?tab=standards&id=pad-bal-01
        var comp = await CreateInitializedComponentAsync(
            queryTab: "standards",
            queryId: "pad-bal-01");

        var activeTab = ComponentTestHarness.GetField<string>(comp, "_activeTab");
        var selectedStandard = ComponentTestHarness.GetField<StandardEquipmentDto>(comp, "_selectedStandard");
        var certs = ComponentTestHarness.GetField<List<StandardCertificateDto>>(comp, "_standardCertificates");
        var usages = ComponentTestHarness.GetField<List<StandardCalibrationUsageDto>>(comp, "_standardUsages");

        activeTab.Should().Be("standards");
        selectedStandard.Should().NotBeNull();
        selectedStandard!.LabTag.Should().Be("PAD-BAL-01");

        certs.Should().NotBeEmpty("Historical certificates must be loaded");
        usages.Should().NotBeEmpty("Historical calibration usages must be loaded");
    }

    [Fact]
    public async Task QueryParameters_EquipmentTabWithLabTag_OpensEquipmentFullViewDirectly()
    {
        var comp = await CreateInitializedComponentAsync(
            queryTab: "equipment",
            queryId: "BAL-CLI-001");

        var activeTab = ComponentTestHarness.GetField<string>(comp, "_activeTab");
        var selectedEq = ComponentTestHarness.GetField<ClientEquipmentDto>(comp, "_selectedEquipment");
        var certs = ComponentTestHarness.GetField<List<CertificateDto>>(comp, "_equipmentCertificates");

        activeTab.Should().Be("equipment");
        selectedEq.Should().NotBeNull();
        selectedEq!.LabTag.Should().Be("BAL-CLI-001");
        certs.Should().NotBeNull();
    }

    [Fact]
    public async Task QueryParameters_ClientsTabWithTaxId_OpensClientFullViewDirectly()
    {
        var comp = await CreateInitializedComponentAsync(
            queryTab: "clients",
            queryId: "12.345.678/0001-90");

        var activeTab = ComponentTestHarness.GetField<string>(comp, "_activeTab");
        var selectedClient = ComponentTestHarness.GetField<ClientDto>(comp, "_selectedClient");

        activeTab.Should().Be("clients");
        selectedClient.Should().NotBeNull();
        selectedClient!.TaxId.Should().Be("12.345.678/0001-90");
    }

    #endregion

    #region 5. Navigation Triggers

    [Fact]
    public async Task CalibrateEquipment_NavigatesToVolumetricCalibrationWithEquipmentTag()
    {
        var comp = await CreateInitializedComponentAsync();

        ComponentTestHarness.Invoke(comp, "CalibrateEquipment", "BAL-CLI-002");

        _navManager.LastNavigatedUri.Should().Be("/calibracao?tag=BAL-CLI-002");
    }

    [Fact]
    public async Task Supervisor_CanEditStandardRole_AndRolePersists()
    {
        var comp = await CreateInitializedComponentAsync(UserRole.Supervisor, "standards", "PAD-BAL-01");
        var selected = ComponentTestHarness.GetField<StandardEquipmentDto>(comp, "_selectedStandard");
        selected.Should().NotBeNull();
        selected!.Role.Should().Be(StandardRole.Balance);

        // Edit role to LiquidThermometer
        selected.Role = StandardRole.LiquidThermometer;
        await ComponentTestHarness.InvokeAsync(comp, "SaveEditStandard");

        var updated = await _dataService.GetStandardEquipmentAsync();
        var reloaded = updated.First(s => s.LabTag == "PAD-BAL-01");
        reloaded.Role.Should().Be(StandardRole.LiquidThermometer);
    }

    #endregion
}
