using MediatR;
using TracLabs.Application.Features.StandardEquipment.Commands.CreateStandardEquipment;
using TracLabs.Application.Features.StandardEquipment.DTOs;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;

namespace TracLabs.Application.Features.StandardEquipment.Commands.PutOutOfService;

public record PutStandardOutOfServiceCommand(
    Guid Id,
    string Reason,
    string ReportedBy) : IRequest<StandardEquipmentDto>;

public class PutStandardOutOfServiceCommandHandler : IRequestHandler<PutStandardOutOfServiceCommand, StandardEquipmentDto>
{
    private readonly IStandardEquipmentRepository _standardRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PutStandardOutOfServiceCommandHandler(
        IStandardEquipmentRepository standardRepository,
        IUnitOfWork unitOfWork)
    {
        _standardRepository = standardRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<StandardEquipmentDto> Handle(PutStandardOutOfServiceCommand request, CancellationToken cancellationToken)
    {
        var standard = await _standardRepository.GetByIdAsync(request.Id, cancellationToken);
        if (standard == null)
            throw new KeyNotFoundException($"Equipamento padrão com ID '{request.Id}' não encontrado.");

        standard.PutOutOfService(request.Reason, request.ReportedBy);

        _standardRepository.Update(standard);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreateStandardEquipmentCommandHandler.MapToDto(standard);
    }
}
