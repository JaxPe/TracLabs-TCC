using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using TracLabs.Application.Features.Certificates.DTOs;
using TracLabs.Application.Interfaces.Services;
using TracLabs.Domain.Entities;
using TracLabs.Domain.Enums;

namespace TracLabs.Infrastructure.Services.Pdf;

public class QuestPdfCertificateGenerator : ICertificatePdfGenerator
{
    static QuestPdfCertificateGenerator()
    {
        // Registro da licença comunitária do QuestPDF
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerateVolumeCertificatePdfAsync(
        CertificateReportDto reportData,
        CertificateCustomizationOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reportData);
        ArgumentNullException.ThrowIfNull(options);

        var document = new VolumeCalibrationDocument(reportData, options);
        var pdfBytes = document.GeneratePdf();

        return Task.FromResult(pdfBytes);
    }

    public Task<IReadOnlyList<byte[]>> GenerateVolumeCertificateImagesAsync(
        CertificateReportDto reportData,
        CertificateCustomizationOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reportData);
        ArgumentNullException.ThrowIfNull(options);

        var document = new VolumeCalibrationDocument(reportData, options);
        var images = document.GenerateImages().ToList();

        return Task.FromResult<IReadOnlyList<byte[]>>(images);
    }
}

internal class VolumeCalibrationDocument : IDocument
{
    private readonly CertificateReportDto _data;
    private readonly CertificateCustomizationOptions _options;
    private readonly CultureInfo _ptBr = new("pt-BR");
    private readonly string _primaryColor;

    public VolumeCalibrationDocument(CertificateReportDto data, CertificateCustomizationOptions options)
    {
        _data = data;
        _options = options;
        _primaryColor = !string.IsNullOrWhiteSpace(options.PrimaryColorHex)
            ? options.PrimaryColorHex
            : "#1E3A8A"; // Azul marinho técnico
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Certificado de Calibração - {_data.CertificateNumber}",
        Author = _options.LaboratoryName ?? "TracLabs Metrologia",
        Subject = $"Calibração Gravimétrica de Volume - {_data.EquipmentLabTag}",
        CreationDate = DateTime.UtcNow
    };

    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(28, Unit.Point);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial").FontColor(Colors.Grey.Darken3));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        var labName = _options.LaboratoryName ?? "TracLabs Metrologia e Calibração";
        var labTaxId = _options.LaboratoryTaxId ?? "CNPJ: 00.000.000/0001-00";
        var labAddress = _options.LaboratoryAddress ?? "Rua da Metrologia, 100 - Niterói/RJ - Brasil";
        var labContact = _options.LaboratoryContact ?? "contato@traclabs.com.br | (21) 9999-9999";
        var rbcAccreditation = _options.AccreditationNumber ?? "Laboratório Acreditado pela Cgcre segundo ABNT NBR ISO/IEC 17025 sob nº CRL 0000";

        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                // Coluna Esquerda: Dados do Laboratório
                row.RelativeItem(3).Column(labCol =>
                {
                    if (!string.IsNullOrWhiteSpace(_options.LogoBase64))
                    {
                        try
                        {
                            var logoBytes = Convert.FromBase64String(_options.LogoBase64);
                            labCol.Item().MaxHeight(38).Image(logoBytes);
                        }
                        catch
                        {
                            labCol.Item().Text(labName).Bold().FontSize(13).FontColor(_primaryColor);
                        }
                    }
                    else
                    {
                        labCol.Item().Text(labName).Bold().FontSize(13).FontColor(_primaryColor);
                    }

                    labCol.Item().Text(labTaxId).FontSize(8).FontColor(Colors.Grey.Darken2);
                    labCol.Item().Text(labAddress).FontSize(8).FontColor(Colors.Grey.Darken2);
                    labCol.Item().Text(labContact).FontSize(8).FontColor(Colors.Grey.Darken2);
                    labCol.Item().Text(rbcAccreditation).Italic().FontSize(7.5f).FontColor(Colors.Grey.Darken3);
                });

                // Coluna Direita: Caixa de Identificação do Certificado
                row.RelativeItem(2).Border(1).BorderColor(_primaryColor).Padding(6).Column(certCol =>
                {
                    certCol.Item().AlignCenter().Text("CERTIFICADO DE CALIBRAÇÃO").Bold().FontSize(10).FontColor(_primaryColor);
                    certCol.Item().AlignCenter().Text(_data.CertificateNumber).Bold().FontSize(12).FontColor(Colors.Black);
                    
                    var statusDesc = _data.Status switch
                    {
                        CertificateStatus.Draft => "RASCUNHO / PREVIEW",
                        CertificateStatus.Issued => "EMITIDO OFICIAL",
                        CertificateStatus.Superseded => "SUBSTITUÍDO / CANCELADO",
                        _ => _data.Status.ToString()
                    };

                    certCol.Item().AlignCenter().PaddingTop(2).Text(txt =>
                    {
                        txt.Span("Status: ").FontSize(8);
                        txt.Span(statusDesc).Bold().FontSize(8).FontColor(_data.Status == CertificateStatus.Issued ? Colors.Green.Darken2 : Colors.Red.Darken2);
                    });

                    certCol.Item().AlignCenter().Text($"Emissão: {(_data.IssuedAt.HasValue ? _data.IssuedAt.Value.ToString("dd/MM/yyyy", _ptBr) : "Pendente")}").FontSize(8);
                });
            });

            col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor(_primaryColor);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(8).Column(col =>
        {
            // 1. Dados do Cliente e do Instrumento
            col.Item().Row(row =>
            {
                // Dados do Cliente
                row.RelativeItem().Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(5).Column(c =>
                {
                    c.Item().Text("1. DADOS DO CLIENTE / SOLICITANTE").Bold().FontSize(8.5f).FontColor(_primaryColor);
                    c.Item().Text($"Razão Social / Nome: {_data.ClientName}").FontSize(8);
                    c.Item().Text($"CNPJ / CPF: {_data.ClientTaxId}").FontSize(8);
                    c.Item().Text($"Endereço: {_data.ClientStreet ?? "Não informado"}, {_data.ClientCity ?? ""}-{_data.ClientState ?? ""}").FontSize(8);
                    c.Item().Text($"Contato: {_data.ClientContactPerson ?? "Não informado"}").FontSize(8);
                });

                row.Spacing(6);

                // Dados do Instrumento
                row.RelativeItem().Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(5).Column(c =>
                {
                    c.Item().Text("2. IDENTIFICAÇÃO DO INSTRUMENTO").Bold().FontSize(8.5f).FontColor(_primaryColor);
                    c.Item().Text($"Descrição: {_data.EquipmentName} ({_data.EquipmentInstrumentType})").FontSize(8);
                    c.Item().Text($"Fabricante: {_data.EquipmentManufacturer} | Modelo: {_data.EquipmentModel}").FontSize(8);
                    c.Item().Text($"Nº de Série: {_data.EquipmentSerialNumber} | Tag/Código: {_data.EquipmentLabTag}").FontSize(8);
                    c.Item().Text($"Capacidade Nominal: {_data.NominalVolumeMl.ToString("N2", _ptBr)} {_data.EquipmentUnit}").FontSize(8);
                    c.Item().Text($"Resolução: {_data.EquipmentResolution.ToString("G", _ptBr)} {_data.EquipmentUnit}").FontSize(8);
                });
            });

            // 2. Metadados do Procedimento e Condições Ambientais
            col.Item().PaddingTop(6).Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(5).Column(c =>
            {
                c.Item().Text("3. MÉTODO DE MEDIÇÃO E CONDIÇÕES AMBIENTAIS").Bold().FontSize(8.5f).FontColor(_primaryColor);
                c.Item().Text("Método Gravimétrico conforme ABNT NBR ISO 4787:2013 e Orientação DOC-CGCRE-027 (INMETRO). " +
                              "Massa específica da água determinada pela equação de Tanaka e massa específica do ar calculada pela fórmula de Spieweck.").FontSize(7.8f);

                c.Item().PaddingTop(3).Row(r =>
                {
                    r.RelativeItem().Text($"Data da Calibração: {_data.CalibrationDate.ToString("dd/MM/yyyy", _ptBr)}").FontSize(8);
                    r.RelativeItem().Text($"Temp. Ambiente: {_data.AmbientTemperatureCelsius.ToString("N1", _ptBr)} °C").FontSize(8);
                    r.RelativeItem().Text($"Umidade Relativa: {_data.RelativeHumidityPercent.ToString("N1", _ptBr)} %").FontSize(8);
                    if (_data.AtmosphericPressureHpa.HasValue)
                        r.RelativeItem().Text($"Pressão: {_data.AtmosphericPressureHpa.Value.ToString("N1", _ptBr)} hPa").FontSize(8);
                    if (_data.WaterTemperatureCelsius.HasValue)
                        r.RelativeItem().Text($"Temp. Água: {_data.WaterTemperatureCelsius.Value.ToString("N2", _ptBr)} °C").FontSize(8);
                });
            });

            // 3. Rastreabilidade Metrológica (Padrões Utilizados)
            col.Item().PaddingTop(6).Column(c =>
            {
                c.Item().Text("4. PADRÕES DE REFERÊNCIA UTILIZADOS (RASTREABILIDADE METROLÓGICA RBC)").Bold().FontSize(8.5f).FontColor(_primaryColor);
                c.Item().PaddingTop(2).Table(t =>
                {
                    t.ColumnsDefinition(cd =>
                    {
                        cd.RelativeColumn(3); // Função
                        cd.RelativeColumn(2); // Tag
                        cd.RelativeColumn(3); // Certificado RBC
                        cd.RelativeColumn(3); // Laboratório Emissor
                        cd.RelativeColumn(2); // Correção
                        cd.RelativeColumn(2); // Incerteza (U)
                        cd.RelativeColumn(1.2f); // k
                    });

                    t.Header(h =>
                    {
                        h.Cell().Background(_primaryColor).Padding(3).Text("Padrão / Função").Bold().FontSize(7.5f).FontColor(Colors.White);
                        h.Cell().Background(_primaryColor).Padding(3).Text("Tag Lab").Bold().FontSize(7.5f).FontColor(Colors.White);
                        h.Cell().Background(_primaryColor).Padding(3).Text("Certificado RBC").Bold().FontSize(7.5f).FontColor(Colors.White);
                        h.Cell().Background(_primaryColor).Padding(3).Text("Órgão Emissor").Bold().FontSize(7.5f).FontColor(Colors.White);
                        h.Cell().Background(_primaryColor).Padding(3).Text("Correção").Bold().FontSize(7.5f).FontColor(Colors.White);
                        h.Cell().Background(_primaryColor).Padding(3).Text("Incerteza (U)").Bold().FontSize(7.5f).FontColor(Colors.White);
                        h.Cell().Background(_primaryColor).Padding(3).Text("k").Bold().FontSize(7.5f).FontColor(Colors.White);
                    });

                    foreach (var std in _data.StandardsUsed)
                    {
                        var bg = Colors.White;
                        t.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2.5f).Text(std.Role).FontSize(7.5f);
                        t.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2.5f).Text(std.LabTag).FontSize(7.5f);
                        t.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2.5f).Text(std.CertificateNumber).FontSize(7.5f);
                        t.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2.5f).Text(std.CalibratingLaboratory ?? "RBC").FontSize(7.5f);
                        t.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2.5f).Text(std.Correction.ToString("N4", _ptBr)).FontSize(7.5f);
                        t.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2.5f).Text(std.ExpandedUncertainty.ToString("N4", _ptBr)).FontSize(7.5f);
                        t.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2.5f).Text(std.CoverageFactor.ToString("N2", _ptBr)).FontSize(7.5f);
                    }
                });
            });

            // 4. Resultados da Calibração
            col.Item().PaddingTop(6).Column(c =>
            {
                c.Item().Text("5. RESULTADOS METROLÓGICOS (ISO/IEC 17025)").Bold().FontSize(8.5f).FontColor(_primaryColor);
                c.Item().PaddingTop(2).Table(t =>
                {
                    t.ColumnsDefinition(cd =>
                    {
                        cd.RelativeColumn(2.5f); // Volume Nominal
                        cd.RelativeColumn(3);    // Volume Corrigido V20
                        cd.RelativeColumn(2.5f); // Erro de Medição
                        cd.RelativeColumn(2.5f); // Incerteza U
                        cd.RelativeColumn(1.5f); // Fator k
                        cd.RelativeColumn(2.5f); // Conformidade
                    });

                    t.Header(h =>
                    {
                        h.Cell().Background(_primaryColor).Padding(3).Text("Volume Nominal (mL)").Bold().FontSize(8).FontColor(Colors.White);
                        h.Cell().Background(_primaryColor).Padding(3).Text("Volume Corrigido V₂₀ (mL)").Bold().FontSize(8).FontColor(Colors.White);
                        h.Cell().Background(_primaryColor).Padding(3).Text("Erro de Medição (mL)").Bold().FontSize(8).FontColor(Colors.White);
                        h.Cell().Background(_primaryColor).Padding(3).Text("Incerteza U (mL)").Bold().FontSize(8).FontColor(Colors.White);
                        h.Cell().Background(_primaryColor).Padding(3).Text("Fator k").Bold().FontSize(8).FontColor(Colors.White);
                        h.Cell().Background(_primaryColor).Padding(3).Text("Conclusão").Bold().FontSize(8).FontColor(Colors.White);
                    });

                    var resultText = _data.Result == CalibrationResult.Conforming ? "CONFORME" :
                                     _data.Result == CalibrationResult.NonConforming ? "NÃO CONFORME" : "PENDENTE";
                    var resultColor = _data.Result == CalibrationResult.Conforming ? Colors.Green.Darken2 : Colors.Red.Darken2;

                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(_data.NominalVolumeMl.ToString("N3", _ptBr)).Bold().FontSize(8.5f);
                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(_data.CalculatedVolumeV20Ml.ToString("N4", _ptBr)).Bold().FontSize(8.5f);
                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(_data.SystematicErrorMl.ToString("+0.0000;-0.0000;0.0000", _ptBr)).Bold().FontSize(8.5f);
                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(_data.ExpandedUncertaintyMl.ToString("N4", _ptBr)).Bold().FontSize(8.5f);
                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(_data.CoverageFactor.ToString("N2", _ptBr)).FontSize(8.5f);
                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(resultText).Bold().FontSize(8.5f).FontColor(resultColor);
                });
            });

            // 5. Tabela de Leituras Repetidas (se habilitado)
            if (_options.ShowDetailedPoints && _data.MeasurementPoints.Count > 0)
            {
                col.Item().PaddingTop(6).Column(c =>
                {
                    c.Item().Text("6. REGISTRO DAS CORRIDAS DE MEDIÇÃO INDIVIDUAIS").Bold().FontSize(8f).FontColor(_primaryColor);
                    c.Item().PaddingTop(2).Table(t =>
                    {
                        t.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(1); // Corrida
                            cd.RelativeColumn(3); // Nominal
                            cd.RelativeColumn(3); // Medido
                            cd.RelativeColumn(3); // Erro
                        });

                        t.Header(h =>
                        {
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(2.5f).Text("Corrida").Bold().FontSize(7.5f);
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(2.5f).Text("Volume Nominal (mL)").Bold().FontSize(7.5f);
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(2.5f).Text("Volume Medido V₂₀ (mL)").Bold().FontSize(7.5f);
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(2.5f).Text("Erro Individual (mL)").Bold().FontSize(7.5f);
                        });

                        foreach (var pt in _data.MeasurementPoints)
                        {
                            t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(pt.RunNumber.ToString()).FontSize(7.5f);
                            t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(pt.NominalVolumeMl.ToString("N3", _ptBr)).FontSize(7.5f);
                            t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(pt.IndicatedVolumeMl.ToString("N4", _ptBr)).FontSize(7.5f);
                            t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(2).Text(pt.ErrorMl.ToString("+0.0000;-0.0000;0.0000", _ptBr)).FontSize(7.5f);
                        }
                    });
                });
            }

            // 6. Declarações e Observações Técnicas
            col.Item().PaddingTop(6).Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(5).Column(c =>
            {
                c.Item().Text("7. NOTAS TÉCNICAS E DECLARAÇÕES NORMATIVAS (ABNT NBR ISO/IEC 17025)").Bold().FontSize(8f).FontColor(_primaryColor);
                c.Item().Text("• Os resultados apresentados neste certificado referem-se exclusivamente ao instrumento ensaiado nas condições da medição.").FontSize(7.5f);
                c.Item().Text("• A incerteza expandida relatada é baseada em uma incerteza combinada multiplicada por um fator de abrangência k correspondente a uma probabilidade de abrangência de aproximadamente 95,45% (GUM - JCGM 100:2008).").FontSize(7.5f);
                c.Item().Text("• A reprodução deste documento só pode ser feita de forma integral. A reprodução de partes requer aprovação prévia por escrito deste laboratório.").FontSize(7.5f);

                if (!string.IsNullOrWhiteSpace(_data.Observations))
                {
                    c.Item().PaddingTop(2).Text(txt =>
                    {
                        txt.Span("Observações: ").Bold().FontSize(7.5f);
                        txt.Span(_data.Observations).FontSize(7.5f);
                    });
                }

                if (!string.IsNullOrWhiteSpace(_options.CustomNotes))
                {
                    c.Item().PaddingTop(2).Text(txt =>
                    {
                        txt.Span("Notas Adicionais do Laboratório: ").Bold().FontSize(7.5f);
                        txt.Span(_options.CustomNotes).FontSize(7.5f);
                    });
                }
            });

            // 7. Assinaturas Autorizadas (se habilitado)
            if (_options.ShowSignatures)
            {
                col.Item().PaddingTop(12).Row(r =>
                {
                    r.RelativeItem().Column(sigCol =>
                    {
                        sigCol.Item().AlignCenter().LineHorizontal(0.8f).LineColor(Colors.Grey.Darken1);
                        sigCol.Item().AlignCenter().Text(_data.TechnicianName).Bold().FontSize(8);
                        sigCol.Item().AlignCenter().Text($"Técnico Executor (ID: {_data.TechnicianId})").FontSize(7.5f).FontColor(Colors.Grey.Darken2);
                    });

                    r.Spacing(40);

                    r.RelativeItem().Column(sigCol =>
                    {
                        sigCol.Item().AlignCenter().LineHorizontal(0.8f).LineColor(Colors.Grey.Darken1);
                        sigCol.Item().AlignCenter().Text(_data.SupervisorName ?? "Supervisor Metrológico").Bold().FontSize(8);
                        sigCol.Item().AlignCenter().Text("Responsável Técnico / Supervisor").FontSize(7.5f).FontColor(Colors.Grey.Darken2);
                    });
                });
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
            col.Item().PaddingTop(3).Row(row =>
            {
                row.RelativeItem().Text("TracLabs Metrologia — Sistema Rastreável de Calibração").FontSize(7).FontColor(Colors.Grey.Darken1);
                row.RelativeItem().AlignRight().Text(txt =>
                {
                    txt.Span("Página ").FontSize(7);
                    txt.CurrentPageNumber().FontSize(7);
                    txt.Span(" de ").FontSize(7);
                    txt.TotalPages().FontSize(7);
                });
            });
        });
    }
}
