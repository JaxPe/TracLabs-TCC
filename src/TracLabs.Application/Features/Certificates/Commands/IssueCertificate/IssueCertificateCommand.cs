using MediatR;
using TracLabs.Application.Features.Certificates.DTOs;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Enums;

namespace TracLabs.Application.Features.Certificates.Commands.IssueCertificate;

public record IssueCertificateCommand(
    Guid CertificateId
) : IRequest<CertificateDetailDto>;

public class IssueCertificateCommandHandler : IRequestHandler<IssueCertificateCommand, CertificateDetailDto>
{
    private readonly ICertificateRepository _certificateRepository;
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public IssueCertificateCommandHandler(
        ICertificateRepository certificateRepository,
        IEquipmentRepository equipmentRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _certificateRepository = certificateRepository;
        _equipmentRepository = equipmentRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<CertificateDetailDto> Handle(IssueCertificateCommand request, CancellationToken cancellationToken)
    {
        var cert = await _certificateRepository.GetWithDetailsAsync(request.CertificateId, cancellationToken);
        if (cert == null)
            throw new KeyNotFoundException($"Certificado com ID '{request.CertificateId}' não encontrado.");

        var userId = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException("Usuário supervisor não identificado.");

        // 1. Emite o certificado (valida se o PDF está anexado e se o status é Draft)
        cert.Issue(userId);

        // 2. Atualiza o ciclo de status do equipamento do cliente para CertificateIssued
        var record = cert.CalibrationRecord
            ?? throw new InvalidOperationException("Registro de calibração não associado ao certificado.");

        var equipment = record.Equipment;
        if (equipment != null && equipment.Status == EquipmentStatus.Calibrated)
        {
            equipment.MarkCertificateIssued();
            _equipmentRepository.Update(equipment);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var client = equipment?.Client;
        var firstPoint = record.MeasurementPoints.FirstOrDefault();
        var nominalVolume = firstPoint?.NominalValue ?? equipment?.NominalCapacity ?? equipment?.RangeMax ?? 0m;

        return new CertificateDetailDto(
            Id: cert.Id,
            CertificateNumber: cert.CertificateNumber,
            CalibrationRecordId: record.Id,
            EquipmentId: equipment?.Id ?? Guid.Empty,
            EquipmentTag: equipment?.LabTag ?? string.Empty,
            EquipmentName: equipment?.Name ?? string.Empty,
            ClientId: client?.Id ?? Guid.Empty,
            ClientName: client?.Name ?? string.Empty,
            Status: cert.Status,
            CalibrationDate: record.CalibrationDate,
            NextCalibrationDue: record.NextCalibrationDue,
            IssuedAt: cert.IssuedAt,
            IssuedByUserId: cert.IssuedByUserId,
            PdfFileName: cert.PdfFileName,
            HasPdf: cert.PdfContent != null && cert.PdfContent.Length > 0,
            NominalVolumeMl: nominalVolume,
            SystematicErrorMl: record.MeanError,
            ExpandedUncertaintyMl: record.ExpandedUncertainty,
            CoverageFactor: record.CoverageFactor,
            Result: record.Result,
            Observations: record.Observations
        );
    }
}
