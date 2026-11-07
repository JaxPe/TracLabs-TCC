using TracLabs.Domain.Enums;

namespace TracLabs.Domain.Entities;

/// <summary>
/// Represents a measurement standard owned and maintained by the laboratory.
/// Used as reference equipment to calibrate client instruments.
/// Incorporates mandatory equipment records per ABNT NBR ISO/IEC 17025:2017 clause 6.4.13.
/// Suporta o fluxo em 2 etapas:
/// 1. Cadastro perpétuo do ativo físico do equipamento padrão (Tag, Modelo, Serial, Resolução, Margem Física);
/// 2. Histórico de certificados de calibração acreditados (1:N), tabela de pontos RBC e curvas de calibração.
/// </summary>
public class StandardEquipment : BaseEntity
{
    // ── Identification (ISO 17025 6.4.13 a & b) ──────────────────────────────────
    
    /// <summary>Nome do padrão (ex: "Balança Analítica XPE205", "Termômetro Digital de Precisão").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Internal laboratory code for this standard (e.g. "PAD-001", "BAL-01").</summary>
    public string LabTag { get; private set; } = string.Empty;
    
    /// <summary>Manufacturer's model number.</summary>
    public string Model { get; private set; } = string.Empty;
    
    /// <summary>Manufacturer/brand name.</summary>
    public string Manufacturer { get; private set; } = string.Empty;
    
    /// <summary>Serial number as stamped on the instrument.</summary>
    public string SerialNumber { get; private set; } = string.Empty;
    
    /// <summary>Instrument type/category (e.g. "Balança Analítica", "Termômetro", "Barômetro").</summary>
    public string InstrumentType { get; private set; } = string.Empty;

    /// <summary>
    /// Versão do software embarcado ou de integração (opcional).
    /// </summary>
    public string? SoftwareVersion { get; private set; }

    /// <summary>
    /// Versão do firmware (opcional — preenchido apenas quando o padrão possuir eletrônica embarcada).
    /// </summary>
    public string? FirmwareVersion { get; private set; }

    // ── Localização, Critérios e Planos (ISO 17025 6.4.13 c, d, f & g) ─────────

    /// <summary>Localização física atual do padrão no laboratório (ex: "Laboratório de Volume - Sala 101").</summary>
    public string CurrentLocation { get; private set; } = string.Empty;

    /// <summary>
    /// Critérios de aceitação metrológica do padrão (ex: "EMA <= 0,0005 g; U <= 0,0002 g").
    /// </summary>
    public string? AcceptanceCriteria { get; private set; }

    /// <summary>Intervalo nominal de calibração em meses (ex: 12 ou 24 meses).</summary>
    public int CalibrationIntervalMonths { get; private set; }

    /// <summary>
    /// Documentação de materiais de referência (ISO 17025 6.4.13 f) — aplicável quando o padrão
    /// for um material de referência certificado (ex: água pura grau analítico, soluções padrão).
    /// </summary>
    public string? ReferenceMaterialDocumentation { get; private set; }

    /// <summary>Plano de manutenção preventiva do equipamento padrão (ISO 17025 6.4.13 g).</summary>
    public string? MaintenancePlan { get; private set; }

    // ── Measurement Range & Margens de Calibração ───────────────────────────────

    /// <summary>Capacidade nominal mínima física do padrão (RangeMin).</summary>
    public decimal RangeMin { get; private set; }

    /// <summary>Capacidade nominal máxima física do padrão (RangeMax).</summary>
    public decimal RangeMax { get; private set; }

    /// <summary>Margem mínima calibrada pelo certificado RBC (CalibrationRangeMin).</summary>
    public decimal? CalibrationRangeMin { get; private set; }

    /// <summary>Margem máxima calibrada pelo certificado RBC (CalibrationRangeMax).</summary>
    public decimal? CalibrationRangeMax { get; private set; }

    // ── Ciclo de Status do Padrão ───────────────────────────────────────────────

    /// <summary>Status atual do padrão no ciclo de vida do laboratório.</summary>
    public StandardEquipmentStatus Status { get; private set; }

    // ── Metrological Data (Snapshot do certificado ativo para compatibilidade) ───
    
    /// <summary>
    /// The systematic measurement error of this standard (erro sistemático).
    /// Error = Indicated Value - Reference Value.
    /// </summary>
    public decimal Error { get; private set; }
    
    /// <summary>
    /// The correction to apply to readings (correção = -Erro).
    /// Applied to compensate for the systematic error.
    /// </summary>
    public decimal Correction { get; private set; }
    
    /// <summary>
    /// Expanded measurement uncertainty at k=2 (approximately 95% confidence level).
    /// </summary>
    public decimal ExpandedUncertainty { get; private set; }
    
    /// <summary>Coverage factor used (typically k=2 for ~95% confidence).</summary>
    public decimal CoverageFactor { get; private set; }

    /// <summary>
    /// Instrument resolution / scale division (menor divisão da escala ou resolução digital).
    /// Fundamental para cálculo da componente de incerteza por distribuição retangular (d / (2*sqrt(3))).
    /// </summary>
    public decimal Resolution { get; private set; }
    
    // ── Calibration Validity ────────────────────────────────────────────────────
    
    /// <summary>Date this standard was last calibrated.</summary>
    public DateTime LastCalibrationDate { get; private set; }
    
    /// <summary>Date this standard's calibration expires. Must be monitored.</summary>
    public DateTime CalibrationDueDate { get; private set; }
    
    /// <summary>Certificate number from the accredited lab that calibrated this standard.</summary>
    public string CalibrationCertificateNumber { get; private set; } = string.Empty;
    
    /// <summary>Name of the accredited lab that performed this standard's calibration.</summary>
    public string CalibratingLaboratory { get; private set; } = string.Empty;

    // ── Histórico de Certificados de Calibração (1:N) ───────────────────────────

    /// <summary>Coleção histórica de todos os certificados de calibração emitidos para este padrão.</summary>
    public IReadOnlyCollection<StandardCertificate> Certificates => _certificates.AsReadOnly();
    private readonly List<StandardCertificate> _certificates = new();

    // ── Computed Properties ──────────────────────────────────────────────────────
    
    /// <summary>
    /// Indica se o padrão possui certificado de calibração vigente.
    /// </summary>
    public bool IsCalibrationValid
    {
        get
        {
            var activeCert = GetActiveCertificate();
            if (activeCert != null)
                return activeCert.IsValid;

            return DateTime.UtcNow.Date <= CalibrationDueDate.Date;
        }
    }
    
    /// <summary>Dias restantes até a expiração da calibração vigente.</summary>
    public int DaysUntilExpiry
    {
        get
        {
            var activeCert = GetActiveCertificate();
            var dueDate = activeCert?.CalibrationDueDate ?? CalibrationDueDate;
            return (dueDate.Date - DateTime.UtcNow.Date).Days;
        }
    }

    /// <summary>
    /// Indica se o padrão está liberado para ser utilizado em novas calibrações.
    /// Requer status Calibrated E calibração vigente.
    /// </summary>
    public bool IsUsableForCalibration => Status == StandardEquipmentStatus.Calibrated && IsCalibrationValid;

    // ── Histórico de Manutenções e Ocorrências (ISO 17025 6.4.13 g & h) ─────────

    public IReadOnlyCollection<EquipmentMaintenanceRecord> MaintenanceHistory =>
        _maintenanceHistory.AsReadOnly();
    private readonly List<EquipmentMaintenanceRecord> _maintenanceHistory = new();

    public IReadOnlyCollection<EquipmentOccurrenceRecord> OccurrenceHistory =>
        _occurrenceHistory.AsReadOnly();
    private readonly List<EquipmentOccurrenceRecord> _occurrenceHistory = new();
    
    // EF Core constructor
    private StandardEquipment() { }
    
    /// <summary>
    /// Construtor completo com dados de certificado inicial (retrocompatibilidade).
    /// </summary>
    public StandardEquipment(
        string labTag, string model, string manufacturer, string serialNumber,
        string instrumentType, decimal error, decimal expandedUncertainty,
        decimal coverageFactor, DateTime lastCalibrationDate, DateTime calibrationDueDate,
        string certificateNumber, string calibratingLaboratory,
        decimal resolution = 0m,
        string? name = null,
        string? softwareVersion = null,
        string? firmwareVersion = null,
        string? currentLocation = null,
        string? acceptanceCriteria = null,
        int calibrationIntervalMonths = 12,
        string? referenceMaterialDocumentation = null,
        string? maintenancePlan = null,
        StandardEquipmentStatus status = StandardEquipmentStatus.Calibrated,
        decimal rangeMin = 0m,
        decimal rangeMax = 0m,
        decimal? calibrationRangeMin = null,
        decimal? calibrationRangeMax = null)
    {
        if (coverageFactor <= 0)
            throw new ArgumentException("Coverage factor must be positive.", nameof(coverageFactor));
        if (calibrationDueDate <= lastCalibrationDate)
            throw new ArgumentException("Due date must be after last calibration date.");
        if (resolution < 0)
            throw new ArgumentException("Resolution cannot be negative.", nameof(resolution));
        if (calibrationIntervalMonths <= 0)
            throw new ArgumentException("Calibration interval months must be positive.", nameof(calibrationIntervalMonths));
            
        LabTag = labTag;
        Model = model;
        Manufacturer = manufacturer;
        SerialNumber = serialNumber;
        InstrumentType = instrumentType;
        Error = error;
        Correction = -error;
        ExpandedUncertainty = expandedUncertainty;
        CoverageFactor = coverageFactor;
        LastCalibrationDate = lastCalibrationDate;
        CalibrationDueDate = calibrationDueDate;
        CalibrationCertificateNumber = certificateNumber;
        CalibratingLaboratory = calibratingLaboratory;
        Resolution = resolution;

        Name = string.IsNullOrWhiteSpace(name) ? instrumentType : name;
        SoftwareVersion = softwareVersion;
        FirmwareVersion = firmwareVersion;
        CurrentLocation = string.IsNullOrWhiteSpace(currentLocation) ? "Laboratório de Calibração" : currentLocation;
        AcceptanceCriteria = acceptanceCriteria;
        CalibrationIntervalMonths = calibrationIntervalMonths;
        ReferenceMaterialDocumentation = referenceMaterialDocumentation;
        MaintenancePlan = maintenancePlan;
        Status = status;

        RangeMin = rangeMin;
        RangeMax = rangeMax;
        CalibrationRangeMin = calibrationRangeMin;
        CalibrationRangeMax = calibrationRangeMax;
    }

    /// <summary>
    /// Construtor para o fluxo Etapa 1: Cadastro de Ativo Físico do Equipamento Padrão.
    /// </summary>
    public StandardEquipment(
        string labTag,
        string model,
        string manufacturer,
        string serialNumber,
        string instrumentType,
        decimal resolution,
        decimal rangeMin,
        decimal rangeMax,
        decimal? calibrationRangeMin = null,
        decimal? calibrationRangeMax = null,
        string? name = null,
        string? currentLocation = null,
        string? acceptanceCriteria = null,
        int calibrationIntervalMonths = 12,
        string? maintenancePlan = null)
    {
        if (string.IsNullOrWhiteSpace(labTag))
            throw new ArgumentException("Lab tag cannot be empty.", nameof(labTag));
        if (resolution < 0)
            throw new ArgumentException("Resolution cannot be negative.", nameof(resolution));
        if (calibrationIntervalMonths <= 0)
            throw new ArgumentException("Calibration interval months must be positive.", nameof(calibrationIntervalMonths));

        LabTag = labTag;
        Name = string.IsNullOrWhiteSpace(name) ? instrumentType : name;
        Model = model;
        Manufacturer = manufacturer;
        SerialNumber = serialNumber;
        InstrumentType = instrumentType;
        Resolution = resolution;
        RangeMin = rangeMin;
        RangeMax = rangeMax;
        CalibrationRangeMin = calibrationRangeMin;
        CalibrationRangeMax = calibrationRangeMax;
        CurrentLocation = string.IsNullOrWhiteSpace(currentLocation) ? "Laboratório de Calibração" : currentLocation;
        AcceptanceCriteria = acceptanceCriteria;
        CalibrationIntervalMonths = calibrationIntervalMonths;
        MaintenancePlan = maintenancePlan;
        Status = StandardEquipmentStatus.PendingCalibration;
    }
    
    // ── Domain Methods: Gestão de Certificados e Curvas ──────────────────────────

    /// <summary>
    /// Adiciona um novo certificado de calibração ao histórico do padrão (Etapa 2).
    /// Se o novo certificado for ativo, desativa os anteriores e sincroniza os parâmetros metrológicos.
    /// </summary>
    public void AddCertificate(StandardCertificate certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        if (certificate.IsActive)
        {
            foreach (var existing in _certificates.Where(c => c.IsActive))
            {
                existing.Deactivate();
            }

            SyncWithActiveCertificate(certificate);
        }

        _certificates.Add(certificate);
        MarkAsUpdated();
    }

    /// <summary>
    /// Retorna o certificado de calibração atualmente ativo e vigente para o padrão.
    /// </summary>
    public StandardCertificate? GetActiveCertificate()
    {
        return _certificates.FirstOrDefault(c => c.IsActive && c.IsValid) 
            ?? _certificates.FirstOrDefault(c => c.IsActive);
    }

    private void SyncWithActiveCertificate(StandardCertificate cert)
    {
        CalibrationCertificateNumber = cert.CertificateNumber;
        CalibratingLaboratory = cert.CalibratingLaboratory;
        LastCalibrationDate = cert.CalibrationDate;
        CalibrationDueDate = cert.CalibrationDueDate;
        CalibrationRangeMin = cert.CalibrationRangeMin;
        CalibrationRangeMax = cert.CalibrationRangeMax;

        if (cert.Points.Count > 0)
        {
            Error = -cert.Points.Average(p => p.Correction);
            Correction = cert.Points.Average(p => p.Correction);
            ExpandedUncertainty = cert.Points.Max(p => p.ExpandedUncertainty);
            CoverageFactor = cert.Points.FirstOrDefault()?.CoverageFactor ?? 2.0m;
        }

        if (cert.IsValid)
        {
            Status = StandardEquipmentStatus.Calibrated;
        }
        else
        {
            Status = StandardEquipmentStatus.PendingCalibration;
        }
    }

    /// <summary>Atualiza as informações cadastrais do padrão (ativo físico).</summary>
    public void UpdateDetails(
        string labTag,
        string name,
        string model,
        string manufacturer,
        string serialNumber,
        string instrumentType,
        decimal resolution,
        decimal rangeMin,
        decimal rangeMax,
        int calibrationIntervalMonths,
        string? softwareVersion,
        string? firmwareVersion,
        string? currentLocation,
        string? acceptanceCriteria,
        string? maintenancePlan,
        string? referenceMaterialDocumentation)
    {
        if (string.IsNullOrWhiteSpace(labTag))
            throw new ArgumentException("Lab tag cannot be empty.", nameof(labTag));
        if (rangeMin > rangeMax)
            throw new ArgumentException("Range minimum cannot be greater than range maximum.");
        if (resolution < 0)
            throw new ArgumentException("Resolution cannot be negative.");
        if (calibrationIntervalMonths <= 0)
            throw new ArgumentException("Calibration interval must be positive.");

        LabTag = labTag;
        Name = string.IsNullOrWhiteSpace(name) ? instrumentType : name;
        Model = model;
        Manufacturer = manufacturer;
        SerialNumber = serialNumber;
        InstrumentType = instrumentType;
        Resolution = resolution;
        RangeMin = rangeMin;
        RangeMax = rangeMax;
        CalibrationIntervalMonths = calibrationIntervalMonths;
        SoftwareVersion = softwareVersion;
        FirmwareVersion = firmwareVersion;
        CurrentLocation = string.IsNullOrWhiteSpace(currentLocation) ? CurrentLocation : currentLocation;
        AcceptanceCriteria = acceptanceCriteria;
        MaintenancePlan = maintenancePlan;
        ReferenceMaterialDocumentation = referenceMaterialDocumentation;

        MarkAsUpdated();
    }

    /// <summary>Define ou atualiza as margens nominais e calibradas do padrão.</summary>
    public void SetRanges(decimal rangeMin, decimal rangeMax, decimal? calRangeMin = null, decimal? calRangeMax = null)
    {
        RangeMin = rangeMin;
        RangeMax = rangeMax;
        CalibrationRangeMin = calRangeMin;
        CalibrationRangeMax = calRangeMax;
        MarkAsUpdated();
    }

    /// <summary>Verifica se um valor medido está dentro da margem de calibração do padrão.</summary>
    public bool IsWithinCalibrationRange(decimal value)
    {
        var min = CalibrationRangeMin ?? RangeMin;
        var max = CalibrationRangeMax ?? RangeMax;
        return value >= min && value <= max;
    }

    /// <summary>Atualiza a localização física atual do padrão.</summary>
    public void UpdateLocation(string newLocation)
    {
        if (string.IsNullOrWhiteSpace(newLocation))
            throw new ArgumentException("A localização não pode ser vazia.", nameof(newLocation));

        CurrentLocation = newLocation;
        MarkAsUpdated();
    }

    /// <summary>Atualiza versões de software e firmware do padrão (opcionais).</summary>
    public void UpdateSoftwareVersions(string? softwareVersion, string? firmwareVersion)
    {
        SoftwareVersion = softwareVersion;
        FirmwareVersion = firmwareVersion;
        MarkAsUpdated();
    }

    /// <summary>Define ou atualiza o critério formal de aceitação do padrão.</summary>
    public void SetAcceptanceCriteria(string? acceptanceCriteria)
    {
        AcceptanceCriteria = acceptanceCriteria;
        MarkAsUpdated();
    }

    /// <summary>
    /// Envia o padrão para calibração em laboratório acreditado externo (RBC/INMETRO).
    /// </summary>
    public void SendToCalibration(string calibratingLaboratory, string? notes = null)
    {
        if (Status == StandardEquipmentStatus.InCalibration)
            throw new InvalidOperationException("O equipamento padrão já está em processo de calibração.");

        if (string.IsNullOrWhiteSpace(calibratingLaboratory))
            throw new ArgumentException("O laboratório executor deve ser informado.", nameof(calibratingLaboratory));

        CalibratingLaboratory = calibratingLaboratory;
        Status = StandardEquipmentStatus.InCalibration;

        if (!string.IsNullOrWhiteSpace(notes))
        {
            AddMaintenanceRecord(
                DateTime.UtcNow,
                MaintenanceType.CalibrationAdjustment,
                $"Envio para calibração externa no laboratório {calibratingLaboratory}. Observações: {notes}",
                "Responsável Técnico");
        }

        MarkAsUpdated();
    }

    /// <summary>
    /// Registra o retorno da calibração externa com o novo certificado metrológico.
    /// </summary>
    public void RegisterCalibrationResult(
        decimal newError,
        decimal newExpandedUncertainty,
        decimal coverageFactor,
        DateTime calibrationDate,
        DateTime dueDate,
        string newCertificateNumber,
        string calibratingLaboratory,
        decimal? newResolution = null,
        bool meetsAcceptanceCriteria = true)
    {
        if (dueDate <= calibrationDate)
            throw new ArgumentException("Due date must be after calibration date.");
        if (coverageFactor <= 0)
            throw new ArgumentException("Coverage factor must be positive.", nameof(coverageFactor));
        if (newResolution.HasValue && newResolution.Value < 0)
            throw new ArgumentException("Resolution cannot be negative.", nameof(newResolution));
        if (string.IsNullOrWhiteSpace(newCertificateNumber))
            throw new ArgumentException("Certificate number cannot be empty.", nameof(newCertificateNumber));

        Error = newError;
        Correction = -newError;
        ExpandedUncertainty = newExpandedUncertainty;
        CoverageFactor = coverageFactor;
        LastCalibrationDate = calibrationDate;
        CalibrationDueDate = dueDate;
        CalibrationCertificateNumber = newCertificateNumber;
        CalibratingLaboratory = calibratingLaboratory;

        if (newResolution.HasValue)
            Resolution = newResolution.Value;

        if (meetsAcceptanceCriteria)
        {
            Status = StandardEquipmentStatus.Calibrated;
        }
        else
        {
            Status = StandardEquipmentStatus.OutOfService;
            ReportOccurrence(
                calibrationDate,
                OccurrenceType.Malfunction,
                $"Padrão reprovado na análise de critérios de aceitação após calibração (Certificado {newCertificateNumber}).",
                "Equipamento colocado fora de serviço e segregado em quarentena.",
                "Responsável da Qualidade");
        }

        MarkAsUpdated();
    }

    /// <summary>
    /// Método legado de compatibilidade para atualização dos dados de calibração.
    /// </summary>
    public void UpdateCalibrationData(
        decimal newError, decimal newExpandedUncertainty, decimal coverageFactor,
        DateTime calibrationDate, DateTime dueDate,
        string newCertificateNumber, string calibratingLaboratory,
        decimal? newResolution = null)
    {
        RegisterCalibrationResult(
            newError, newExpandedUncertainty, coverageFactor, calibrationDate, dueDate,
            newCertificateNumber, calibratingLaboratory, newResolution, meetsAcceptanceCriteria: true);
    }

    /// <summary>
    /// Coloca o equipamento padrão fora de serviço (quarentena, defeito ou descontinuação).
    /// </summary>
    public void PutOutOfService(string reason, string reportedBy)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("O motivo para colocar fora de serviço deve ser especificado.", nameof(reason));

        Status = StandardEquipmentStatus.OutOfService;
        ReportOccurrence(
            DateTime.UtcNow,
            OccurrenceType.Damage,
            $"Padrão colocado fora de serviço. Motivo: {reason}",
            "Equipamento segregado de uso metrológico.",
            reportedBy);

        MarkAsUpdated();
    }

    /// <summary>
    /// Reativa o equipamento padrão para o serviço metrológico após validação/reparo.
    /// </summary>
    public void Reactivate(string reason, string approvedBy)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A justificativa de reativação deve ser especificada.", nameof(reason));

        if (!IsCalibrationValid)
        {
            Status = StandardEquipmentStatus.PendingCalibration;
        }
        else
        {
            Status = StandardEquipmentStatus.Calibrated;
        }

        AddMaintenanceRecord(
            DateTime.UtcNow,
            MaintenanceType.Preventive,
            $"Equipamento reativado para serviço. Justificativa: {reason}",
            approvedBy,
            $"Status resultante: {Status}");

        MarkAsUpdated();
    }

    /// <summary>
    /// Valida o prazo de calibração do padrão. Se estiver expirado e com status Calibrated,
    /// altera automaticamente o status para PendingCalibration.
    /// </summary>
    public void CheckCalibrationValidity(DateTime? referenceDate = null)
    {
        var checkDate = (referenceDate ?? DateTime.UtcNow).Date;
        var dueDate = GetActiveCertificate()?.CalibrationDueDate ?? CalibrationDueDate;
        if (checkDate > dueDate.Date && Status == StandardEquipmentStatus.Calibrated)
        {
            Status = StandardEquipmentStatus.PendingCalibration;
            MarkAsUpdated();
        }
    }

    /// <summary>Registra uma manutenção preventiva/corretiva no padrão (ISO 17025 6.4.13 g).</summary>
    public EquipmentMaintenanceRecord AddMaintenanceRecord(
        DateTime date, MaintenanceType type, string description, string performedBy, string? resultSummary = null)
    {
        var record = new EquipmentMaintenanceRecord(
            date, type, description, performedBy, resultSummary, standardEquipmentId: Id);
        _maintenanceHistory.Add(record);
        MarkAsUpdated();
        return record;
    }

    /// <summary>Registra uma anomalia, dano ou reparo sofrido pelo padrão (ISO 17025 6.4.13 h).</summary>
    public EquipmentOccurrenceRecord ReportOccurrence(
        DateTime date, OccurrenceType type, string description, string actionTaken, string reportedBy, DateTime? resolvedDate = null)
    {
        var record = new EquipmentOccurrenceRecord(
            date, type, description, actionTaken, reportedBy, resolvedDate, standardEquipmentId: Id);
        _occurrenceHistory.Add(record);
        MarkAsUpdated();
        return record;
    }
}
