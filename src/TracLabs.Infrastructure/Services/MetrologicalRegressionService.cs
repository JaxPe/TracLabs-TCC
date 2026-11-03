using TracLabs.Application.Interfaces.Services;
using TracLabs.Domain.Common;
using TracLabs.Domain.ValueObjects;

namespace TracLabs.Infrastructure.Services;

/// <summary>
/// Implementação de alta precisão do serviço de regressão linear (Mínimos Quadrados / OLS)
/// e interpolação linear segmentada (piecewise linear), delegando os cálculos para a classe de domínio MetrologicalMath.
/// </summary>
public class MetrologicalRegressionService : IMetrologicalRegressionService
{
    public (decimal Slope, decimal Intercept, decimal R2) CalculateLinearRegression(IEnumerable<(decimal X, decimal Y)> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        var pts = points.ToList();

        if (pts.Count < 2)
            throw new InvalidOperationException("São necessários no mínimo 2 pontos para calcular a regressão linear.");

        var (slope, intercept, r2) = MetrologicalMath.CalculateLinearRegression(pts);

        if (!slope.HasValue || !intercept.HasValue || !r2.HasValue)
        {
            throw new InvalidOperationException("Não é possível calcular a reta de regressão linear quando todos os pontos possuem o mesmo valor de X (reta vertical com inclinação indefinida).");
        }

        return (slope.Value, intercept.Value, r2.Value);
    }

    public (decimal Slope, decimal Intercept, decimal R2) CalculateLinearRegression(IEnumerable<StandardCalibrationPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        return CalculateLinearRegression(points.Select(p => (p.IndicatedValue, p.Correction)));
    }

    public decimal InterpolatePiecewise(IEnumerable<(decimal X, decimal Y)> points, decimal targetX)
    {
        ArgumentNullException.ThrowIfNull(points);
        var pts = points.ToList();
        return MetrologicalMath.InterpolatePiecewise(pts, targetX);
    }

    public decimal InterpolatePiecewise(IEnumerable<StandardCalibrationPoint> points, decimal targetX)
    {
        ArgumentNullException.ThrowIfNull(points);
        return InterpolatePiecewise(points.Select(p => (p.IndicatedValue, p.Correction)), targetX);
    }
}
