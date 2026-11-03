using TracLabs.Domain.Common;
using TracLabs.Domain.Enums;
using TracLabs.Domain.ValueObjects;

namespace TracLabs.Domain.Entities;

/// <summary>
/// Representa um certificado de calibração acreditado (RBC/Inmetro) emitido para um equipamento padrão.
/// Mantém a rastreabilidade metrológica completa, a tabela de pontos calibrados e os parâmetros
/// ajustados da curva de calibração (regressão linear y = ax + b ou interpolação segmentada).
/// </summary>
public class StandardCertificate : BaseEntity
{
    public Guid StandardEquipmentId { get; private set; }
    public StandardEquipment? StandardEquipment { get; private set; }

    /// <summary>Número identificador do certificado (ex: "CAL-RBC-2026-089").</summary>
    public string CertificateNumber { get; private set; } = string.Empty;

    /// <summary>Nome do laboratório acreditado que realizou a calibração do padrão.</summary>
    public string CalibratingLaboratory { get; private set; } = string.Empty;

    /// <summary>Data em que o padrão foi calibrado.</summary>
    public DateTime CalibrationDate { get; private set; }

    /// <summary>Data de expiração da validade metrológica deste certificado.</summary>
    public DateTime CalibrationDueDate { get; private set; }

    /// <summary>Margem mínima calibrada pelo certificado RBC (ex: 15.0°C ou 100 µL).</summary>
    public decimal CalibrationRangeMin { get; private set; }

    /// <summary>Margem máxima calibrada pelo certificado RBC (ex: 25.0°C ou 1000 µL).</summary>
    public decimal CalibrationRangeMax { get; private set; }

    /// <summary>Indica se este certificado é o atualmente vigente e ativo para uso do padrão.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Método matemático de resolução dos pontos (Regressão Linear, Segmentada ou Média).</summary>
    public CurveFitType FitType { get; private set; } = CurveFitType.LinearRegression;

    /// <summary>Diretriz de incerteza no ponto de uso (Pior Incerteza do Certificado vs Interpolada).</summary>
    public UncertaintyPolicy UncertaintyPolicy { get; private set; } = UncertaintyPolicy.WorstCase;

    // ── Coeficientes Ajustados da Reta y = ax + b (Mínimos Quadrados / OLS) ─────────
    
    /// <summary>Coeficiente angular da reta de regressão (a / inclinação / Slope).</summary>
    public decimal? Slope { get; private set; }

    /// <summary>Coeficiente linear da reta de regressão (b / intersecção / Intercept).</summary>
    public decimal? Intercept { get; private set; }

    /// <summary>Coeficiente de determinação da regressão (R² / R-quadrado).</summary>
    public decimal? R2 { get; private set; }

    // ── Tabela de Pontos de Calibração do Certificado ───────────────────────────────
    public List<StandardCalibrationPoint> Points { get; private set; } = new();

    /// <summary>Indica se o certificado ainda está dentro do prazo de validade.</summary>
    public bool IsValid => DateTime.UtcNow.Date <= CalibrationDueDate.Date;

    // Construtor protegido para o EF Core
    private StandardCertificate() { }

    public StandardCertificate(
        Guid standardEquipmentId,
        string certificateNumber,
        string calibratingLaboratory,
        DateTime calibrationDate,
        DateTime calibrationDueDate,
        decimal calibrationRangeMin,
        decimal calibrationRangeMax,
        CurveFitType fitType = CurveFitType.LinearRegression,
        UncertaintyPolicy uncertaintyPolicy = UncertaintyPolicy.WorstCase,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(certificateNumber))
            throw new ArgumentException("O número do certificado não pode ser vazio.", nameof(certificateNumber));
        if (string.IsNullOrWhiteSpace(calibratingLaboratory))
            throw new ArgumentException("O laboratório calibrador deve ser informado.", nameof(calibratingLaboratory));
        if (calibrationDueDate <= calibrationDate)
            throw new ArgumentException("A validade do certificado deve ser posterior à data de calibração.");
        if (calibrationRangeMin > calibrationRangeMax)
            throw new ArgumentException("A margem mínima de calibração não pode ser maior que a máxima.");

        StandardEquipmentId = standardEquipmentId;
        CertificateNumber = certificateNumber;
        CalibratingLaboratory = calibratingLaboratory;
        CalibrationDate = calibrationDate;
        CalibrationDueDate = calibrationDueDate;
        CalibrationRangeMin = calibrationRangeMin;
        CalibrationRangeMax = calibrationRangeMax;
        FitType = fitType;
        UncertaintyPolicy = uncertaintyPolicy;
        IsActive = isActive;
    }

    /// <summary>
    /// Construtor de conveniência que recebe diretamente a tabela de pontos discretos e deduz as faixas mínima e máxima.
    /// </summary>
    public StandardCertificate(
        Guid standardEquipmentId,
        string certificateNumber,
        string calibratingLaboratory,
        DateTime calibrationDate,
        DateTime calibrationDueDate,
        IEnumerable<StandardCalibrationPoint> points,
        CurveFitType fitType = CurveFitType.LinearRegression,
        UncertaintyPolicy uncertaintyPolicy = UncertaintyPolicy.WorstCase,
        bool isActive = true)
        : this(
            standardEquipmentId,
            certificateNumber,
            calibratingLaboratory,
            calibrationDate,
            calibrationDueDate,
            points?.Any() == true ? points.Min(p => p.IndicatedValue) : 0m,
            points?.Any() == true ? points.Max(p => p.IndicatedValue) : 0m,
            fitType,
            uncertaintyPolicy,
            isActive)
    {
        if (points != null)
        {
            SetPoints(points);
        }
    }

    /// <summary>
    /// Adiciona os pontos da tabela de calibração e recalcula automaticamente a curva ajustada.
    /// Mantém os pontos ordenados por valor indicado para otimização de resolução.
    /// </summary>
    public void SetPoints(IEnumerable<StandardCalibrationPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        Points = points.OrderBy(p => p.IndicatedValue).ToList();

        if (Points.Count > 0)
        {
            // Ajusta as margens de calibração automaticamente aos extremos dos pontos se não informados
            if (CalibrationRangeMin == 0 && CalibrationRangeMax == 0)
            {
                CalibrationRangeMin = Points.Min(p => p.IndicatedValue);
                CalibrationRangeMax = Points.Max(p => p.IndicatedValue);
            }

            RecalculateRegressionCurve();
        }

        MarkAsUpdated();
    }

    /// <summary>
    /// Recalcula os coeficientes da reta y = ax + b e R² usando Mínimos Quadrados Ordinários (OLS),
    /// delegando o algoritmo em passagem única (1N) para a classe de domínio MetrologicalMath.
    /// </summary>
    public void RecalculateRegressionCurve()
    {
        if (Points.Count < 2)
        {
            Slope = null;
            Intercept = null;
            R2 = null;
            return;
        }

        var (slope, intercept, r2) = MetrologicalMath.CalculateLinearRegression(
            Points.Select(p => (p.IndicatedValue, p.Correction)).ToList());

        Slope = slope;
        Intercept = intercept;
        R2 = r2;
    }

    /// <summary>
    /// Resolve a correção e a incerteza do padrão metrológico no ponto operacional de ensaio,
    /// emitindo Warning de auditoria caso o ponto extrapole a faixa calibrada ou os pontos discretos reais.
    /// </summary>
    public ResolvedStandardCorrection ResolveCorrectionAt(decimal operatingValue)
    {
        if (Points.Count == 0)
        {
            bool isExtrapolatedEmpty = operatingValue < CalibrationRangeMin || operatingValue > CalibrationRangeMax;
            string? warnEmpty = isExtrapolatedEmpty
                ? $"O valor operacional de ensaio ({operatingValue}) está fora da faixa acreditada RBC ({CalibrationRangeMin} a {CalibrationRangeMax}) do certificado '{CertificateNumber}'."
                : null;

            return new ResolvedStandardCorrection(
                operatingValue,
                0m,
                0m,
                2.0m,
                isExtrapolatedEmpty,
                warnEmpty,
                "NoPointsFallback");
        }

        decimal pointsMin = Points.Min(p => p.IndicatedValue);
        decimal pointsMax = Points.Max(p => p.IndicatedValue);
        bool outsideDeclaredRange = operatingValue < CalibrationRangeMin || operatingValue > CalibrationRangeMax;
        bool outsidePointsRange = operatingValue < pointsMin || operatingValue > pointsMax;

        bool isExtrapolated;
        string? warningMessage = null;
        decimal calculatedCorrection;
        string methodUsed = FitType.ToString();

        if (FitType == CurveFitType.LinearRegression)
        {
            // Na regressão linear OLS, a reta é um modelo global válido em todo o escopo calibrado formal
            isExtrapolated = outsideDeclaredRange;
            if (isExtrapolated)
            {
                warningMessage = $"O valor operacional de ensaio ({operatingValue}) está fora da faixa acreditada RBC ({CalibrationRangeMin} a {CalibrationRangeMax}) do certificado '{CertificateNumber}'.";
            }

            if (Slope.HasValue && Intercept.HasValue)
            {
                calculatedCorrection = Math.Round((Slope.Value * operatingValue) + Intercept.Value, 6);
                methodUsed = $"LinearRegression(y = {Slope.Value}x + {Intercept.Value})";
            }
            else
            {
                // Fallback para interpolação se a reta for vertical/indefinida
                calculatedCorrection = MetrologicalMath.InterpolatePiecewise(
                    Points.Select(p => (p.IndicatedValue, p.Correction)).ToList(), operatingValue);
                methodUsed = "PiecewiseLinearFallback";
            }
        }
        else if (FitType == CurveFitType.PiecewiseLinear && Points.Count >= 2)
        {
            // Na interpolação por partes, ultrapassar os pontos medidos reais é extrapolação cega
            isExtrapolated = outsidePointsRange || outsideDeclaredRange;
            if (outsidePointsRange)
            {
                warningMessage = $"O valor operacional de ensaio ({operatingValue}) extrapola a faixa dos pontos medidos ({pointsMin} a {pointsMax}) na interpolação por partes do certificado '{CertificateNumber}', gerando perda de rastreabilidade metrológica.";
            }
            else if (outsideDeclaredRange)
            {
                warningMessage = $"O valor operacional de ensaio ({operatingValue}) está fora da faixa acreditada RBC ({CalibrationRangeMin} a {CalibrationRangeMax}) do certificado '{CertificateNumber}'.";
            }

            calculatedCorrection = MetrologicalMath.InterpolatePiecewise(
                Points.Select(p => (p.IndicatedValue, p.Correction)).ToList(), operatingValue);
            methodUsed = "PiecewiseLinear";
        }
        else
        {
            // Média fixa das correções dos pontos
            isExtrapolated = outsideDeclaredRange;
            if (isExtrapolated)
            {
                warningMessage = $"O valor operacional de ensaio ({operatingValue}) está fora da faixa acreditada RBC ({CalibrationRangeMin} a {CalibrationRangeMax}) do certificado '{CertificateNumber}'.";
            }

            calculatedCorrection = Math.Round(Points.Average(p => p.Correction), 6);
            methodUsed = "FixedAverage";
        }

        // Resolução da Incerteza Expandida
        decimal calculatedUncertainty;
        decimal coverageFactor = Points.FirstOrDefault()?.CoverageFactor ?? 2.0m;

        if (UncertaintyPolicy == UncertaintyPolicy.WorstCase || Points.Count < 2)
        {
            calculatedUncertainty = Points.Max(p => p.ExpandedUncertainty);
        }
        else
        {
            calculatedUncertainty = MetrologicalMath.InterpolateUncertainty(
                Points.Select(p => (p.IndicatedValue, p.ExpandedUncertainty)).ToList(), operatingValue);
        }

        return new ResolvedStandardCorrection(
            operatingValue,
            calculatedCorrection,
            calculatedUncertainty,
            coverageFactor,
            isExtrapolated,
            warningMessage,
            methodUsed);
    }

    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    public void UpdateSettings(CurveFitType fitType, UncertaintyPolicy policy)
    {
        FitType = fitType;
        UncertaintyPolicy = policy;
        RecalculateRegressionCurve();
        MarkAsUpdated();
    }
}
