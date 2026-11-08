using TracLabs.Frontend.Components.Pages;
using TracLabs.Frontend.Models;
using TracLabs.Frontend.Models.Enums;
using TracLabs.Frontend.Services;
using TracLabs.Frontend.Services.Auth;
using TracLabs.Frontend.Services.Notification;
using Xunit;

namespace TracLabs.Frontend.Tests;

public class NotificationModalServiceTests
{
    [Fact]
    public void ShowError_SetsProperties_AndTriggersOnChange()
    {
        // Arrange
        var service = new NotificationModalService();
        var eventTriggered = false;
        service.OnChange += () => eventTriggered = true;

        // Act
        service.ShowError("Erro Crítico", "Falha de validação na API.", "Stack trace detalhado");

        // Assert
        Assert.True(eventTriggered);
        Assert.True(service.CurrentNotification.IsVisible);
        Assert.Equal(NotificationSeverity.Error, service.CurrentNotification.Severity);
        Assert.Equal("Erro Crítico", service.CurrentNotification.Title);
        Assert.Equal("Falha de validação na API.", service.CurrentNotification.Message);
        Assert.Equal("Stack trace detalhado", service.CurrentNotification.TechnicalDetails);
        Assert.Equal("Fechar", service.CurrentNotification.ConfirmButtonText);
    }

    [Fact]
    public void ShowWarning_SetsSeverityWarning_AndTriggersOnChange()
    {
        // Arrange
        var service = new NotificationModalService();
        var eventTriggered = false;
        service.OnChange += () => eventTriggered = true;

        // Act
        service.ShowWarning("Atenção", "Equipamento com calibracao pendente.");

        // Assert
        Assert.True(eventTriggered);
        Assert.True(service.CurrentNotification.IsVisible);
        Assert.Equal(NotificationSeverity.Warning, service.CurrentNotification.Severity);
        Assert.Equal("Atenção", service.CurrentNotification.Title);
        Assert.Equal("Equipamento com calibracao pendente.", service.CurrentNotification.Message);
        Assert.Equal("Entendi", service.CurrentNotification.ConfirmButtonText);
    }

    [Fact]
    public void ShowSuccess_SetsSeveritySuccess_AndTriggersOnChange()
    {
        // Arrange
        var service = new NotificationModalService();
        var eventTriggered = false;
        service.OnChange += () => eventTriggered = true;

        // Act
        service.ShowSuccess("Sucesso", "Operação concluída com êxito.");

        // Assert
        Assert.True(eventTriggered);
        Assert.True(service.CurrentNotification.IsVisible);
        Assert.Equal(NotificationSeverity.Success, service.CurrentNotification.Severity);
        Assert.Equal("Sucesso", service.CurrentNotification.Title);
        Assert.Equal("Operação concluída com êxito.", service.CurrentNotification.Message);
    }

    [Fact]
    public void Close_HidesNotification_AndTriggersOnChange()
    {
        // Arrange
        var service = new NotificationModalService();
        service.ShowError("Título", "Mensagem");
        Assert.True(service.CurrentNotification.IsVisible);

        var eventTriggered = false;
        service.OnChange += () => eventTriggered = true;

        // Act
        service.Close();

        // Assert
        Assert.True(eventTriggered);
        Assert.False(service.CurrentNotification.IsVisible);
    }

    [Fact]
    public async Task CalibracaoVolumetrica_SaveAndGenerateCertificate_WhenEquipmentAlreadyIssued_ShowsFloatingWarning()
    {
        // Arrange
        var modalService = new NotificationModalService();
        var dataService = new MockMetrologyDataService();
        var userContext = new UserContextService();
        var navManager = new TestNavigationManager();

        var component = new CalibracaoVolumetrica();
        ComponentTestHarness.InitializeComponent(component);
        ComponentTestHarness.SetProperty(component, "DataService", dataService);
        ComponentTestHarness.SetProperty(component, "UserContext", userContext);
        ComponentTestHarness.SetProperty(component, "Navigation", navManager);
        ComponentTestHarness.SetProperty(component, "NotificationModal", modalService);

        await ComponentTestHarness.InvokeAsync(component, "OnInitializedAsync");

        // Simula seleção de um equipamento que já possui certificado emitido
        var issuedEquipment = new ClientEquipmentDto
        {
            Id = Guid.NewGuid().ToString(),
            LabTag = "BALAO-CLI-100",
            Description = "Balão Volumétrico 100 mL",
            Status = EquipmentStatus.CertificateIssued
        };
        ComponentTestHarness.SetField(component, "_selectedEquipment", issuedEquipment);

        // Act
        await ComponentTestHarness.InvokeAsync(component, "SaveAndGenerateCertificate");

        // Assert
        Assert.True(modalService.CurrentNotification.IsVisible);
        Assert.Equal(NotificationSeverity.Warning, modalService.CurrentNotification.Severity);
        Assert.Contains("Apenas equipamentos no status 'Recebido' ou 'Aprovado'", modalService.CurrentNotification.Message);
    }
}
