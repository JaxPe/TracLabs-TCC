using MediatR;
using TracLabs.Application.Features.StandardEquipment.Commands.CreateStandardEquipment;
using TracLabs.Application.Features.StandardEquipment.DTOs;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;

namespace TracLabs.Application.Features.StandardEquipment.Commands.SendToCalibration;

public record SendStandardToCalibrationCommand(
    Guid Id,
    string CalibratingLaboratory,
    string? Notes = null) : IRequest<StandardEquipmentDto>;

public class SendStandardToCalibrationCommandHandler : IRequestHandler<SendStandardToCalibrationCommand, StandardEquipmentDto>
{
    private readonly IStandardEquipmentRepository _standardRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SendStandardToCalibrationCommandHandler(
        IStandardEquipmentRepository standardRepository,
        IUnitOfWork unitOfWork)
    {
        _standardRepository = standardRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<StandardEquipmentDto> Handle(SendStandardToCalibrationCommand request, CancellationToken cancellationToken)
    {
        var standard = await _standardRepository.GetByIdAsync(request.Id, cancellationToken);
        if (standard == null)
            throw new KeyNotFoundException($"Equipamento padrão com ID '{request.Id}' não encontrado.");

        standard.SendToCalibration(request.CalibratingLaboratory, request.Notes);

        _standardRepository.Update(standard);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreateStandardEquipmentCommandHandler.MapToDto(standard);
    }
}
