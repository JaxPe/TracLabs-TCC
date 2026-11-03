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
    /// </summary>
    public void SetPoints(IEnumerable<StandardCalibrationPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        Points = points.OrderBy(p => p.Order).ThenBy(p => p.IndicatedValue).ToList();

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
    /// espelhando as funções SLOPE e INTERCEPT do Excel.
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

        int n = Points.Count;
        decimal sumX = Points.Sum(p => p.IndicatedValue);
        decimal sumY = Points.Sum(p => p.Correction);
        decimal sumXY = Points.Sum(p => p.IndicatedValue * p.Correction);
        decimal sumX2 = Points.Sum(p => p.IndicatedValue * p.IndicatedValue);
        decimal sumY2 = Points.Sum(p => p.Correction * p.Correction);

        decimal denominator = (n * sumX2) - (sumX * sumX);
        if (denominator == 0)
        {
            Slope = 0;
            Intercept = sumY / n;
            R2 = 1m;
            return;
        }

        decimal slope = ((n * sumXY) - (sumX * sumY)) / denominator;
        decimal intercept = (sumY - (slope * sumX)) / n;

        decimal numeratorR2 = (n * sumXY) - (sumX * sumY);
        decimal denomR2 = denominator * ((n * sumY2) - (sumY * sumY));
        decimal r2 = denomR2 > 0 ? (numeratorR2 * numeratorR2) / denomR2 : 1m;

        Slope = Math.Round(slope, 6);
        Intercept = Math.Round(intercept, 6);
        R2 = Math.Round(r2, 6);
    }

    /// <summary>
    /// Resolve a correção e a incerteza do padrão metrológico no ponto operacional de ensaio,
    /// emitindo Warning caso o ponto esteja fora da margem de calibração do certificado.
    /// </summary>
    public ResolvedStandardCorrection ResolveCorrectionAt(decimal operatingValue)
    {
        bool isExtrapolated = operatingValue < CalibrationRangeMin || operatingValue > CalibrationRangeMax;
        string? warningMessage = isExtrapolated
            ? $"O valor operacional de ensaio ({operatingValue}) está fora da margem de calibração do certificado '{CertificateNumber}' ({CalibrationRangeMin} a {CalibrationRangeMax})."
            : null;

        decimal calculatedCorrection = 0m;
        string methodUsed = FitType.ToString();

        if (Points.Count == 0)
        {
            return new ResolvedStandardCorrection(
                operatingValue,
                0m,
                0m,
                2.0m,
                isExtrapolated,
                warningMessage,
                "NoPointsFallback");
        }

        if (FitType == CurveFitType.LinearRegression && Slope.HasValue && Intercept.HasValue)
        {
            calculatedCorrection = Math.Round((Slope.Value * operatingValue) + Intercept.Value, 6);
            methodUsed = $"LinearRegression(y = {Slope.Value}x + {Intercept.Value})";
        }
        else if (FitType == CurveFitType.PiecewiseLinear && Points.Count >= 2)
        {
            calculatedCorrection = InterpolatePiecewise(operatingValue);
            methodUsed = "PiecewiseLinear";
        }
        else
        {
            // Média fixa das correções dos pontos
            calculatedCorrection = Math.Round(Points.Average(p => p.Correction), 6);
            methodUsed = "FixedAverage";
        }

        // Resolução da Incerteza Expandida
        decimal calculatedUncertainty = 0m;
        decimal coverageFactor = Points.FirstOrDefault()?.CoverageFactor ?? 2.0m;

        if (UncertaintyPolicy == UncertaintyPolicy.WorstCase || Points.Count < 2)
        {
            calculatedUncertainty = Points.Max(p => p.ExpandedUncertainty);
        }
        else
        {
            calculatedUncertainty = InterpolateUncertainty(operatingValue);
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

    private decimal InterpolatePiecewise(decimal targetX)
    {
        var sorted = Points.OrderBy(p => p.IndicatedValue).ToList();

        // Se estiver abaixo do primeiro ponto
        if (targetX <= sorted[0].IndicatedValue)
        {
            var p0 = sorted[0];
            var p1 = sorted[1];
            decimal slope = (p1.Correction - p0.Correction) / (p1.IndicatedValue - p0.IndicatedValue);
            return Math.Round(p0.Correction + slope * (targetX - p0.IndicatedValue), 6);
        }

        // Se estiver acima do último ponto
        if (targetX >= sorted[^1].IndicatedValue)
        {
            var pPrev = sorted[^2];
            var pLast = sorted[^1];
            decimal slope = (pLast.Correction - pPrev.Correction) / (pLast.IndicatedValue - pPrev.IndicatedValue);
            return Math.Round(pLast.Correction + slope * (targetX - pLast.IndicatedValue), 6);
        }

        // Interpolação no segmento correspondente
        for (int i = 0; i < sorted.Count - 1; i++)
        {
            var pA = sorted[i];
            var pB = sorted[i + 1];

            if (targetX >= pA.IndicatedValue && targetX <= pB.IndicatedValue)
            {
                decimal span = pB.IndicatedValue - pA.IndicatedValue;
                if (span == 0) return pA.Correction;

                decimal fraction = (targetX - pA.IndicatedValue) / span;
                return Math.Round(pA.Correction + fraction * (pB.Correction - pA.Correction), 6);
            }
        }

        return sorted[^1].Correction;
    }

    private decimal InterpolateUncertainty(decimal targetX)
    {
        var sorted = Points.OrderBy(p => p.IndicatedValue).ToList();
        if (targetX <= sorted[0].IndicatedValue) return sorted[0].ExpandedUncertainty;
        if (targetX >= sorted[^1].IndicatedValue) return sorted[^1].ExpandedUncertainty;

        for (int i = 0; i < sorted.Count - 1; i++)
        {
            var pA = sorted[i];
            var pB = sorted[i + 1];

            if (targetX >= pA.IndicatedValue && targetX <= pB.IndicatedValue)
            {
                decimal span = pB.IndicatedValue - pA.IndicatedValue;
                if (span == 0) return pA.ExpandedUncertainty;

                decimal fraction = (targetX - pA.IndicatedValue) / span;
                return Math.Round(pA.ExpandedUncertainty + fraction * (pB.ExpandedUncertainty - pA.ExpandedUncertainty), 6);
            }
        }

        return sorted.Max(p => p.ExpandedUncertainty);
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
