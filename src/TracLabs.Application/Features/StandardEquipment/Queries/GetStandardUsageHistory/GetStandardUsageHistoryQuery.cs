using MediatR;
using TracLabs.Application.Features.StandardEquipment.DTOs;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Enums;

namespace TracLabs.Application.Features.StandardEquipment.Queries.GetStandardUsageHistory;

public record GetStandardUsageHistoryQuery(Guid StandardId) : IRequest<IReadOnlyList<StandardCalibrationUsageDto>>;

public class GetStandardUsageHistoryQueryHandler : IRequestHandler<GetStandardUsageHistoryQuery, IReadOnlyList<StandardCalibrationUsageDto>>
{
    private readonly ICalibrationRepository _calibrationRepository;

    public GetStandardUsageHistoryQueryHandler(ICalibrationRepository calibrationRepository)
    {
        _calibrationRepository = calibrationRepository;
    }

    public async Task<IReadOnlyList<StandardCalibrationUsageDto>> Handle(GetStandardUsageHistoryQuery request, CancellationToken cancellationToken)
    {
        var records = await _calibrationRepository.GetByStandardEquipmentIdAsync(request.StandardId, cancellationToken);

        return records.Select(c =>
        {
            var su = c.StandardsUsed.FirstOrDefault(s => s.StandardEquipmentId == request.StandardId);
            var role = su?.Role ?? StandardRole.Balance;
            var opValue = su?.OperatingValue ?? 0m;

            return new StandardCalibrationUsageDto(
                Id: su?.Id ?? c.Id,
                StandardId: request.StandardId,
                CalibrationId: c.Id,
                CalibrationDate: c.CalibrationDate,
                CustomerName: c.Equipment?.Client?.Name ?? "Cliente Não Informado",
                EquipmentTag: c.Equipment?.LabTag ?? "N/A",
                EquipmentDescription: c.Equipment?.Name ?? "Instrumento",
                NominalVolume: c.Equipment?.NominalCapacity ?? 0m,
                Unit: c.Equipment?.Unit ?? "mL",
                TechnicianName: string.IsNullOrWhiteSpace(c.TechnicianName) ? (string.IsNullOrWhiteSpace(c.TechnicianId) ? "Técnico Metrologista" : c.TechnicianId) : c.TechnicianName,
                CertificateNumberIssued: c.Certificate?.CertificateNumber ?? "Pendente",
                RoleUsed: role,
                OperatingValue: opValue,
                CertificateStatus: c.Certificate?.Status == CertificateStatus.Issued ? "Emitido" :
                                   c.Certificate?.Status == CertificateStatus.Superseded ? "Substituído" : "Rascunho"
            );
        }).ToList();
    }
}
