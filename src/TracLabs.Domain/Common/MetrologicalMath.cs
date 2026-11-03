namespace TracLabs.Domain.Common;

/// <summary>
/// Métodos matemáticos e estatísticos de alta precisão para metrologia e calibração (VIM / ABNT NBR ISO/IEC 17025).
/// Centraliza algoritmos puros de regressão linear (OLS), consolidação de ciclos de histerese e interpolação linear segmentada.
/// </summary>
public static class MetrologicalMath
{
    /// <summary>
    /// Consolida ensaios de calibração que possuem leituras repetidas na mesma indicação (ciclos de histerese),
    /// agrupando por X e calculando a média aritmética de Y, retornando uma lista estritamente crescente em X.
    /// </summary>
    public static IReadOnlyList<(decimal X, decimal Y)> ConsolidateHysteresis(IEnumerable<(decimal X, decimal Y)> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        return points
            .GroupBy(p => p.X)
            .OrderBy(g => g.Key)
            .Select(g => (X: g.Key, Y: Math.Round(g.Average(p => p.Y), 6)))
            .ToList();
    }

    /// <summary>
    /// Consolida pontos de incerteza que possuem a mesma indicação X (ciclos de histerese),
    /// adotando a pior incerteza expandida max(U) no ponto para assegurar conservadorismo metrológico conforme o GUM.
    /// </summary>
    public static IReadOnlyList<(decimal X, decimal U)> ConsolidateHysteresisUncertainty(IEnumerable<(decimal X, decimal U)> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        return points
            .GroupBy(p => p.X)
            .OrderBy(g => g.Key)
            .Select(g => (X: g.Key, U: g.Max(p => p.U)))
            .ToList();
    }

    /// <summary>
    /// Calcula a reta de regressão linear (y = ax + b) e o coeficiente R² via Mínimos Quadrados Ordinários (OLS)
    /// em uma única passagem computacional (1N), espelhando as funções SLOPE, INTERCEPT e RSQ do Excel.
    /// Retorna null caso n &lt; 2 ou todos os pontos possuam o mesmo X (reta vertical com inclinação indefinida).
    /// </summary>
    public static (decimal? Slope, decimal? Intercept, decimal? R2) CalculateLinearRegression(
        IReadOnlyCollection<(decimal X, decimal Y)> points)
    {
        if (points == null || points.Count < 2)
            return (null, null, null);

        int n = points.Count;
        decimal sumX = 0m, sumY = 0m, sumXY = 0m, sumX2 = 0m, sumY2 = 0m;

        foreach (var p in points)
        {
            sumX += p.X;
            sumY += p.Y;
            sumXY += p.X * p.Y;
            sumX2 += p.X * p.X;
            sumY2 += p.Y * p.Y;
        }

        decimal denominator = (n * sumX2) - (sumX * sumX);
        if (denominator == 0)
        {
            // Reta vertical: inclinação matematicamente indefinida
            return (null, null, null);
        }

        decimal slope = ((n * sumXY) - (sumX * sumY)) / denominator;
        decimal intercept = (sumY - (slope * sumX)) / n;

        decimal numeratorR2 = (n * sumXY) - (sumX * sumY);
        decimal denomR2 = denominator * ((n * sumY2) - (sumY * sumY));
        decimal r2 = denomR2 > 0 ? (numeratorR2 * numeratorR2) / denomR2 : 1m;

        return (Math.Round(slope, 6), Math.Round(intercept, 6), Math.Round(r2, 6));
    }

    /// <summary>
    /// Interpola linearmente entre segmentos discretos.
    /// Aplica consolidação de histerese e previne divisão por zero em qualquer ponto da curva.
    /// </summary>
    public static decimal InterpolatePiecewise(IReadOnlyCollection<(decimal X, decimal Y)> points, decimal targetX)
    {
        var consolidated = ConsolidateHysteresis(points);
        if (consolidated.Count == 0)
            throw new InvalidOperationException("Nenhum ponto fornecido para interpolação.");

        if (consolidated.Count == 1)
            return consolidated[0].Y;

        // Extrapolação à esquerda (inclinação do primeiro segmento consolidado)
        if (targetX <= consolidated[0].X)
        {
            var p0 = consolidated[0];
            var p1 = consolidated[1];
            decimal span = p1.X - p0.X;
            if (span == 0) return p0.Y;

            decimal slope = (p1.Y - p0.Y) / span;
            return Math.Round(p0.Y + slope * (targetX - p0.X), 6);
        }

        // Extrapolação à direita (inclinação do último segmento consolidado)
        if (targetX >= consolidated[^1].X)
        {
            var pPrev = consolidated[^2];
            var pLast = consolidated[^1];
            decimal span = pLast.X - pPrev.X;
            if (span == 0) return pLast.Y;

            decimal slope = (pLast.Y - pPrev.Y) / span;
            return Math.Round(pLast.Y + slope * (targetX - pLast.X), 6);
        }

        // Interpolação no segmento correspondente
        for (int i = 0; i < consolidated.Count - 1; i++)
        {
            var pA = consolidated[i];
            var pB = consolidated[i + 1];

            if (targetX >= pA.X && targetX <= pB.X)
            {
                decimal span = pB.X - pA.X;
                if (span == 0) return pA.Y;

                decimal fraction = (targetX - pA.X) / span;
                return Math.Round(pA.Y + fraction * (pB.Y - pA.Y), 6);
            }
        }

        return consolidated[^1].Y;
    }

    /// <summary>
    /// Interpola linearmente a incerteza de medição entre segmentos discretos.
    /// Consolida leituras de histerese com a máxima incerteza do ponto e assegura proteção contra divisão por zero.
    /// </summary>
    public static decimal InterpolateUncertainty(IReadOnlyCollection<(decimal X, decimal U)> points, decimal targetX)
    {
        var consolidated = ConsolidateHysteresisUncertainty(points);
        if (consolidated.Count == 0)
            return 0m;

        if (consolidated.Count == 1)
            return consolidated[0].U;

        if (targetX <= consolidated[0].X)
            return consolidated[0].U;

        if (targetX >= consolidated[^1].X)
            return consolidated[^1].U;

        for (int i = 0; i < consolidated.Count - 1; i++)
        {
            var pA = consolidated[i];
            var pB = consolidated[i + 1];

            if (targetX >= pA.X && targetX <= pB.X)
            {
                decimal span = pB.X - pA.X;
                if (span == 0) return pA.U;

                decimal fraction = (targetX - pA.X) / span;
                return Math.Round(pA.U + fraction * (pB.U - pA.U), 6);
            }
        }

        return consolidated.Max(p => p.U);
    }
}
