using MediatR;
using TracLabs.Application.Features.StandardEquipment.DTOs;
using TracLabs.Application.Interfaces.Repositories;

namespace TracLabs.Application.Features.StandardEquipment.Queries.GetStandardCertificates;

public record GetStandardCertificatesQuery(Guid StandardId) : IRequest<IReadOnlyList<StandardCertificateDto>>;

public class GetStandardCertificatesQueryHandler : IRequestHandler<GetStandardCertificatesQuery, IReadOnlyList<StandardCertificateDto>>
{
    private readonly IStandardEquipmentRepository _standardEquipmentRepository;

    public GetStandardCertificatesQueryHandler(IStandardEquipmentRepository standardEquipmentRepository)
    {
        _standardEquipmentRepository = standardEquipmentRepository;
    }

    public async Task<IReadOnlyList<StandardCertificateDto>> Handle(GetStandardCertificatesQuery request, CancellationToken cancellationToken)
    {
        var standard = await _standardEquipmentRepository.GetWithCertificatesAsync(request.StandardId, cancellationToken);
        if (standard == null)
            return Array.Empty<StandardCertificateDto>();

        return standard.Certificates
            .OrderByDescending(c => c.CalibrationDate)
            .Select(c => new StandardCertificateDto(
                Id: c.Id,
                StandardEquipmentId: c.StandardEquipmentId,
                CertificateNumber: c.CertificateNumber,
                CalibratingLaboratory: c.CalibratingLaboratory,
                CalibrationDate: c.CalibrationDate,
                CalibrationDueDate: c.CalibrationDueDate,
                CalibrationRangeMin: c.CalibrationRangeMin,
                CalibrationRangeMax: c.CalibrationRangeMax,
                IsActive: c.IsActive,
                FitType: c.FitType,
                UncertaintyPolicy: c.UncertaintyPolicy,
                Slope: c.Slope,
                Intercept: c.Intercept,
                R2: c.R2,
                Points: c.Points.Select(p => new StandardCalibrationPointDto(
                    p.Order,
                    p.IndicatedValue,
                    p.StandardError,
                    p.Correction,
                    p.ExpandedUncertainty,
                    p.CoverageFactor
                )).ToList()
            )).ToList();
    }
}
