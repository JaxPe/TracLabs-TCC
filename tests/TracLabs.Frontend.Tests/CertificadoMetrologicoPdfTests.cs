using System.Text;
using FluentAssertions;
using TracLabs.Frontend.Components.Pages;
using TracLabs.Frontend.Models;
using TracLabs.Frontend.Models.Enums;
using TracLabs.Frontend.Services;
using TracLabs.Frontend.Services.Auth;
using Xunit;

namespace TracLabs.Frontend.Tests;

/// <summary>
/// Unit and component tests for CertificadoMetrologico.razor PDF integration:
/// - Official PDF download via JSInterop (`pdfViewer.downloadFileFromBase64`)
/// - In-place PDF viewer modal (`OpenPdfViewerModalAsync`, base64 data URL)
/// - View PDF in new browser tab (`pdfViewer.viewPdfFromBase64`)
/// - PDF byte caching and lifecycle invalidation on status transitions
/// - Safe error handling when PDF cannot be generated
/// - Window print invocation
/// </summary>
public class CertificadoMetrologicoPdfTests
{
    private readonly MockMetrologyDataService _dataService = new();
    private readonly TestNavigationManager _navManager = new();
    private readonly TestJsRuntime _jsRuntime = new();

    private async Task<CertificadoMetrologico> CreateInitializedComponentAsync(string? certId = null)
    {
        var component = new CertificadoMetrologico();
        ComponentTestHarness.InitializeComponent(component);
        ComponentTestHarness.SetProperty(component, "DataService", _dataService);
        ComponentTestHarness.SetProperty(component, "Navigation", _navManager);
        ComponentTestHarness.SetProperty(component, "JS", _jsRuntime);

        if (!string.IsNullOrEmpty(certId))
        {
            ComponentTestHarness.SetProperty(component, "Id", certId);
        }

        await ComponentTestHarness.InvokeAsync(component, "OnInitializedAsync");
        return component;
    }

    [Fact]
    public async Task DownloadPdfAsync_FetchesPdfBytes_AndInvokesJsDownloadFunction()
    {
        var comp = await CreateInitializedComponentAsync("CERT-2026-00042");

        await ComponentTestHarness.InvokeAsync(comp, "DownloadPdfAsync");

        // Verify JS function was called
        _jsRuntime.HasInvoked("pdfViewer.downloadFileFromBase64").Should().BeTrue();

        var args = _jsRuntime.GetInvocationArgs("pdfViewer.downloadFileFromBase64");
        args.Should().NotBeNull();
        args!.Length.Should().Be(3);

        var fileName = args[0] as string;
        var base64Data = args[1] as string;
        var mimeType = args[2] as string;

        fileName.Should().Be("Certificado_CERT-2026-00042.pdf");
        mimeType.Should().Be("application/pdf");

        // Verify base64 decodes to valid PDF-1.4 binary content
        base64Data.Should().NotBeNullOrWhiteSpace();
        var bytes = Convert.FromBase64String(base64Data!);
        var header = Encoding.ASCII.GetString(bytes.Take(4).ToArray());
        header.Should().Be("%PDF");
    }

    [Fact]
    public async Task OpenPdfViewerModalAsync_GeneratesDataUrl_AndSetsModalVisible()
    {
        var comp = await CreateInitializedComponentAsync("CERT-2026-00042");

        await ComponentTestHarness.InvokeAsync(comp, "OpenPdfViewerModalAsync");

        var showModal = ComponentTestHarness.GetField<bool>(comp, "_showPdfModal");
        var pdfDataUrl = ComponentTestHarness.GetField<string>(comp, "_pdfDataUrl");
        var errorMessage = ComponentTestHarness.GetField<string>(comp, "_pdfErrorMessage");

        showModal.Should().BeTrue();
        pdfDataUrl.Should().StartWith("data:application/pdf;base64,");
        errorMessage.Should().BeNull();

        // Close modal
        ComponentTestHarness.Invoke(comp, "ClosePdfModal");
        var showModalAfter = ComponentTestHarness.GetField<bool>(comp, "_showPdfModal");
        showModalAfter.Should().BeFalse();
    }

    [Fact]
    public async Task ViewPdfInNewTabAsync_FetchesPdfBytes_AndInvokesJsViewFunction()
    {
        var comp = await CreateInitializedComponentAsync("CERT-2026-00042");

        await ComponentTestHarness.InvokeAsync(comp, "ViewPdfInNewTabAsync");

        _jsRuntime.HasInvoked("pdfViewer.viewPdfFromBase64").Should().BeTrue();

        var args = _jsRuntime.GetInvocationArgs("pdfViewer.viewPdfFromBase64");
        args.Should().NotBeNull();
        args!.Length.Should().Be(2);

        var base64Data = args[0] as string;
        var mimeType = args[1] as string;

        mimeType.Should().Be("application/pdf");
        base64Data.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task PdfCaching_ReusesCachedBytes_AcrossMultipleActions()
    {
        var comp = await CreateInitializedComponentAsync("CERT-2026-00042");

        // Action 1: Download
        await ComponentTestHarness.InvokeAsync(comp, "DownloadPdfAsync");
        var cachedBytes1 = ComponentTestHarness.GetField<byte[]>(comp, "_cachedPdfBytes");
        var cachedCertId1 = ComponentTestHarness.GetField<string>(comp, "_cachedPdfCertId");

        cachedBytes1.Should().NotBeNull();
        cachedCertId1.Should().NotBeNull();

        // Action 2: Open modal
        await ComponentTestHarness.InvokeAsync(comp, "OpenPdfViewerModalAsync");
        var cachedBytes2 = ComponentTestHarness.GetField<byte[]>(comp, "_cachedPdfBytes");

        // Must point to identical cached byte array instance
        ReferenceEquals(cachedBytes1, cachedBytes2).Should().BeTrue("PDF bytes must be cached to prevent redundant serialization");
    }

    [Fact]
    public async Task PdfCache_Invalidates_OnCertificateApproval()
    {
        var comp = await CreateInitializedComponentAsync("CERT-2026-00042");

        // Load PDF bytes into cache
        await ComponentTestHarness.InvokeAsync(comp, "OpenPdfViewerModalAsync");
        var cachedBefore = ComponentTestHarness.GetField<byte[]>(comp, "_cachedPdfBytes");
        cachedBefore.Should().NotBeNull();

        // Approve certificate -> cache must be invalidated
        await ComponentTestHarness.InvokeAsync(comp, "ApproveCertificate");

        var cachedAfter = ComponentTestHarness.GetField<byte[]>(comp, "_cachedPdfBytes");
        cachedAfter.Should().BeNull("Cache must be invalidated so subsequent download reflects approved status");
    }

    [Fact]
    public async Task PdfCache_Invalidates_OnCertificateRejection()
    {
        var comp = await CreateInitializedComponentAsync("CERT-2026-00042");

        await ComponentTestHarness.InvokeAsync(comp, "DownloadPdfAsync");
        ComponentTestHarness.GetField<byte[]>(comp, "_cachedPdfBytes").Should().NotBeNull();

        await ComponentTestHarness.InvokeAsync(comp, "RejectDraft");

        ComponentTestHarness.GetField<byte[]>(comp, "_cachedPdfBytes").Should().BeNull();
    }

    [Fact]
    public async Task PdfCache_Invalidates_OnCertificateCancellation()
    {
        var comp = await CreateInitializedComponentAsync("CERT-2026-00042");

        await ComponentTestHarness.InvokeAsync(comp, "DownloadPdfAsync");
        ComponentTestHarness.GetField<byte[]>(comp, "_cachedPdfBytes").Should().NotBeNull();

        ComponentTestHarness.SetField(comp, "_cancelReason", "Erro na temperatura da água verificado em auditoria");
        await ComponentTestHarness.InvokeAsync(comp, "ConfirmCancelCertificate");

        ComponentTestHarness.GetField<byte[]>(comp, "_cachedPdfBytes").Should().BeNull();
    }

    [Fact]
    public async Task PrintCertificate_InvokesWindowPrint()
    {
        var comp = await CreateInitializedComponentAsync("CERT-2026-00042");

        await ComponentTestHarness.InvokeAsync(comp, "PrintCertificate");

        _jsRuntime.HasInvoked("window.print").Should().BeTrue();
    }

    [Fact]
    public async Task WhenCurrentCertNull_PdfActionsExitSafelyWithoutExceptions()
    {
        var comp = await CreateInitializedComponentAsync();
        ComponentTestHarness.SetField(comp, "_currentCert", null);

        var actDownload = async () => await ComponentTestHarness.InvokeAsync(comp, "DownloadPdfAsync");
        var actView = async () => await ComponentTestHarness.InvokeAsync(comp, "OpenPdfViewerModalAsync");
        var actTab = async () => await ComponentTestHarness.InvokeAsync(comp, "ViewPdfInNewTabAsync");

        await actDownload.Should().NotThrowAsync();
        await actView.Should().NotThrowAsync();
        await actTab.Should().NotThrowAsync();
    }
}
