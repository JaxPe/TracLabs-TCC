using MediatR;
using TracLabs.Application.Features.StandardEquipment.Commands.CreateStandardEquipment;
using TracLabs.Application.Features.StandardEquipment.DTOs;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;

namespace TracLabs.Application.Features.StandardEquipment.Commands.ReactivateStandard;

public record ReactivateStandardCommand(
    Guid Id,
    string Reason,
    string ApprovedBy) : IRequest<StandardEquipmentDto>;

public class ReactivateStandardCommandHandler : IRequestHandler<ReactivateStandardCommand, StandardEquipmentDto>
{
    private readonly IStandardEquipmentRepository _standardRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReactivateStandardCommandHandler(
        IStandardEquipmentRepository standardRepository,
        IUnitOfWork unitOfWork)
    {
        _standardRepository = standardRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<StandardEquipmentDto> Handle(ReactivateStandardCommand request, CancellationToken cancellationToken)
    {
        var standard = await _standardRepository.GetByIdAsync(request.Id, cancellationToken);
        if (standard == null)
            throw new KeyNotFoundException($"Equipamento padrão com ID '{request.Id}' não encontrado.");

        standard.Reactivate(request.Reason, request.ApprovedBy);

        _standardRepository.Update(standard);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreateStandardEquipmentCommandHandler.MapToDto(standard);
    }
}
