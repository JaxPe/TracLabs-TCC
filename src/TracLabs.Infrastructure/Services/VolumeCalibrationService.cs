using TracLabs.Application.DTOs.Calibration;
using TracLabs.Application.Interfaces;
using TracLabs.Domain.ValueObjects;

namespace TracLabs.Infrastructure.Services;

public class VolumeCalibrationService : IVolumeCalibrationService
{
    // Simple implementation following common GUM elements:
    // - mean error
    // - max absolute error
    // - standard uncertainty: combine standard's U (convert to uc = U/k)
    //   and resolution contribution (rectangular -> a/sqrt(3)), and
    //   repeatability modeled as std dev of error points (if >0)

    public VolumeCalibrationResultDto Calculate(VolumeCalibrationRequest request)
    {
        if (request.MeasurementPoints == null || request.MeasurementPoints.Count == 0)
            throw new ArgumentException("At least one measurement point is required.");

        var errors = request.MeasurementPoints
            .Select(p => p.IndicatedValue - p.NominalValue)
            .ToList();

        var meanError = errors.Average();
        var maxAbsError = errors.Select(Math.Abs).Max();

        // Standard's combined standard uncertainty (uc_std) from expanded U and k
        var ucStd = request.StandardCoverageFactor > 0
            ? request.StandardExpandedUncertainty / request.StandardCoverageFactor
            : 0m;

        // Resolution contribution (rectangular distribution): a/sqrt(3)
        var resContribution = request.InstrumentResolution / (decimal)Math.Sqrt(3);

        // Repeatability: use sample standard deviation of errors
        decimal repeatability = 0m;
        if (errors.Count > 1)
        {
            var avg = meanError;
            var sumSq = errors.Sum(e => (e - avg) * (e - avg));
            var variance = sumSq / (errors.Count - 1);
            repeatability = (decimal)Math.Sqrt((double)variance);
        }

        // Combine uncertainties (quadratic sum)
        var combinedUc = (decimal)Math.Sqrt(
            (double)(ucStd * ucStd + resContribution * resContribution + repeatability * repeatability)
        );

        // Expanded uncertainty for requested coverage factor
        var expandedU = combinedUc * request.ResultCoverageFactor;

        return new VolumeCalibrationResultDto
        {
            PointsCount = request.MeasurementPoints.Count,
            MeanError = meanError,
            MaxAbsoluteError = maxAbsError,
            CombinedStandardUncertainty = combinedUc,
            ExpandedUncertainty = expandedU,
            CoverageFactor = request.ResultCoverageFactor
        };
    }
}
