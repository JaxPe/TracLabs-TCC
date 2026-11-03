using FluentAssertions;
using TracLabs.Application.Interfaces.Services;
using TracLabs.Domain.ValueObjects;
using TracLabs.Infrastructure.Services;
using Xunit;

namespace TracLabs.Application.Tests;

public class MetrologicalRegressionServiceTests
{
    private readonly IMetrologicalRegressionService _service = new MetrologicalRegressionService();

    [Fact]
    public void CalculateLinearRegression_WithConsultantExcelData_ShouldMatchExpectedSlopeInterceptAndR2()
    {
        // Arrange - Dados idênticos à planilha enviada pelo consultor:
        // Ponto 1: T = 19°C, Correção = 0.1
        // Ponto 2: T = 20°C, Correção = 0.2
        // Ponto 3: T = 21°C, Correção = 0.3
        var points = new List<StandardCalibrationPoint>
        {
            new(1, 19.0m, -0.1m, 0.1m, 0.1m, 2.0m),
            new(2, 20.0m, -0.2m, 0.2m, 0.1m, 2.0m),
            new(3, 21.0m, -0.3m, 0.3m, 0.1m, 2.0m)
        };

        // Act
        var (slope, intercept, r2) = _service.CalculateLinearRegression(points);

        // Assert - Excel: y = 0,1x - 1,8 com R² = 1
        slope.Should().Be(0.1m);
        intercept.Should().Be(-1.8m);
        r2.Should().Be(1.0m);
    }

    [Fact]
    public void CalculateLinearRegression_WithTuples_ShouldReturnExpectedCoefficients()
    {
        // Arrange
        var points = new List<(decimal X, decimal Y)>
        {
            (19.0m, 0.1m),
            (20.0m, 0.2m),
            (21.0m, 0.3m)
        };

        // Act
        var (slope, intercept, r2) = _service.CalculateLinearRegression(points);

        // Assert
        slope.Should().Be(0.1m);
        intercept.Should().Be(-1.8m);
        r2.Should().Be(1.0m);
    }

    [Fact]
    public void InterpolatePiecewise_WithNonLinearPoints_ShouldInterpolateLinearly()
    {
        // Arrange
        var points = new List<(decimal X, decimal Y)>
        {
            (10.0m, 0.10m),
            (20.0m, 0.30m),
            (30.0m, 0.70m)
        };

        // Act - Ponto 15.0m (meio do caminho entre 10 e 20: 0.10 + 0.5 * (0.30 - 0.10) = 0.20)
        decimal result = _service.InterpolatePiecewise(points, 15.0m);

        // Assert
        result.Should().Be(0.20m);
    }

    [Fact]
    public void InterpolatePiecewise_WithCalibrationPoints_ShouldInterpolateLinearly()
    {
        // Arrange
        var points = new List<StandardCalibrationPoint>
        {
            new(1, 10.0m, 0m, 0.10m, 0.05m, 2.0m),
            new(2, 20.0m, 0m, 0.30m, 0.05m, 2.0m),
            new(3, 30.0m, 0m, 0.70m, 0.05m, 2.0m)
        };

        // Act
        decimal result = _service.InterpolatePiecewise(points, 25.0m);

        // Assert - Ponto 25.0m (meio entre 20 e 30: 0.30 + 0.5 * (0.70 - 0.30) = 0.50)
        result.Should().Be(0.50m);
    }

    [Fact]
    public void CalculateLinearRegression_WithLessThan2Points_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var singlePoint = new List<(decimal X, decimal Y)> { (10.0m, 0.1m) };

        // Act
        var act = () => _service.CalculateLinearRegression(singlePoint);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*no mínimo 2 pontos*");
    }

    [Fact]
    public void CalculateLinearRegression_WithAllEqualX_ShouldThrowInvalidOperationException()
    {
        // Arrange - Reta vertical com todos os pontos no mesmo X (denominador == 0)
        var verticalPoints = new List<(decimal X, decimal Y)>
        {
            (20.0m, 0.10m),
            (20.0m, 0.20m),
            (20.0m, 0.30m)
        };

        // Act
        var act = () => _service.CalculateLinearRegression(verticalPoints);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*reta vertical com inclinação indefinida*");
    }

    [Fact]
    public void InterpolatePiecewise_WithHysteresisData_ShouldComputeCorrectAverages()
    {
        // Arrange - Leituras repetidas em subida e descida no mesmo valor de X
        var points = new List<(decimal X, decimal Y)>
        {
            (10.0m, 0.08m),
            (10.0m, 0.12m), // Média = 0.10
            (20.0m, 0.28m),
            (20.0m, 0.32m), // Média = 0.30
            (30.0m, 0.70m)
        };

        // Act - Ponto 10 (exato consolidado) e ponto 15 (meio do caminho: 0.10 + 0.5*(0.30 - 0.10) = 0.20)
        decimal at10 = _service.InterpolatePiecewise(points, 10.0m);
        decimal at15 = _service.InterpolatePiecewise(points, 15.0m);

        // Assert
        at10.Should().Be(0.10m);
        at15.Should().Be(0.20m);
    }
}
