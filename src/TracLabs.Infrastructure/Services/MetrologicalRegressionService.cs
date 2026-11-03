using TracLabs.Application.Interfaces.Services;
using TracLabs.Domain.ValueObjects;

namespace TracLabs.Infrastructure.Services;

/// <summary>
/// Implementação matemática de alta precisão do serviço de regressão linear (Mínimos Quadrados / OLS)
/// e interpolação linear segmentada (piecewise linear), espelhando as funções do Microsoft Excel.
/// </summary>
public class MetrologicalRegressionService : IMetrologicalRegressionService
{
    public (decimal Slope, decimal Intercept, decimal R2) CalculateLinearRegression(IEnumerable<(decimal X, decimal Y)> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        var pts = points.ToList();
        int n = pts.Count;

        if (n < 2)
            throw new InvalidOperationException("São necessários no mínimo 2 pontos para calcular a regressão linear.");

        decimal sumX = pts.Sum(p => p.X);
        decimal sumY = pts.Sum(p => p.Y);
        decimal sumXY = pts.Sum(p => p.X * p.Y);
        decimal sumX2 = pts.Sum(p => p.X * p.X);
        decimal sumY2 = pts.Sum(p => p.Y * p.Y);

        decimal denominator = (n * sumX2) - (sumX * sumX);
        if (denominator == 0)
        {
            return (0m, Math.Round(sumY / n, 6), 1m);
        }

        decimal slope = ((n * sumXY) - (sumX * sumY)) / denominator;
        decimal intercept = (sumY - (slope * sumX)) / n;

        decimal numeratorR2 = (n * sumXY) - (sumX * sumY);
        decimal denomR2 = denominator * ((n * sumY2) - (sumY * sumY));
        decimal r2 = denomR2 > 0 ? (numeratorR2 * numeratorR2) / denomR2 : 1m;

        return (Math.Round(slope, 6), Math.Round(intercept, 6), Math.Round(r2, 6));
    }

    public (decimal Slope, decimal Intercept, decimal R2) CalculateLinearRegression(IEnumerable<StandardCalibrationPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        return CalculateLinearRegression(points.Select(p => (p.IndicatedValue, p.Correction)));
    }

    public decimal InterpolatePiecewise(IEnumerable<(decimal X, decimal Y)> points, decimal targetX)
    {
        ArgumentNullException.ThrowIfNull(points);
        var sorted = points.OrderBy(p => p.X).ToList();

        if (sorted.Count == 0)
            throw new InvalidOperationException("Nenhum ponto fornecido para interpolação.");

        if (sorted.Count == 1)
            return sorted[0].Y;

        // Se o valor estiver à esquerda do primeiro ponto (extrapolação no primeiro segmento)
        if (targetX <= sorted[0].X)
        {
            var p0 = sorted[0];
            var p1 = sorted[1];
            decimal slope = (p1.Y - p0.Y) / (p1.X - p0.X);
            return Math.Round(p0.Y + slope * (targetX - p0.X), 6);
        }

        // Se o valor estiver à direita do último ponto (extrapolação no último segmento)
        if (targetX >= sorted[^1].X)
        {
            var pPrev = sorted[^2];
            var pLast = sorted[^1];
            decimal slope = (pLast.Y - pPrev.Y) / (pLast.X - pPrev.X);
            return Math.Round(pLast.Y + slope * (targetX - pLast.X), 6);
        }

        // Interpolação dentro do intervalo entre dois pontos
        for (int i = 0; i < sorted.Count - 1; i++)
        {
            var pA = sorted[i];
            var pB = sorted[i + 1];

            if (targetX >= pA.X && targetX <= pB.X)
            {
                decimal span = pB.X - pA.X;
                if (span == 0) return pA.Y;

                decimal fraction = (targetX - pA.X) / span;
                return Math.Round(pA.Y + fraction * (pB.Y - pA.Y), 6);
            }
        }

        return sorted[^1].Y;
    }

    public decimal InterpolatePiecewise(IEnumerable<StandardCalibrationPoint> points, decimal targetX)
    {
        ArgumentNullException.ThrowIfNull(points);
        return InterpolatePiecewise(points.Select(p => (p.IndicatedValue, p.Correction)), targetX);
    }
}
