using MediatR;
using TracLabs.Application.Features.StandardEquipment.Commands.CreateStandardEquipment;
using TracLabs.Application.Features.StandardEquipment.DTOs;
using TracLabs.Application.Interfaces.Repositories;
using TracLabs.Domain.Enums;

namespace TracLabs.Application.Features.StandardEquipment.Queries.GetStandardEquipment;

public record GetStandardEquipmentQuery(StandardEquipmentStatus? Status = null) : IRequest<IReadOnlyList<StandardEquipmentDto>>;

public class GetStandardEquipmentQueryHandler : IRequestHandler<GetStandardEquipmentQuery, IReadOnlyList<StandardEquipmentDto>>
{
    private readonly IStandardEquipmentRepository _standardRepository;

    public GetStandardEquipmentQueryHandler(IStandardEquipmentRepository standardRepository)
    {
        _standardRepository = standardRepository;
    }

    public async Task<IReadOnlyList<StandardEquipmentDto>> Handle(GetStandardEquipmentQuery request, CancellationToken cancellationToken)
    {
        var all = await _standardRepository.GetAllAsync(cancellationToken);

        if (request.Status.HasValue)
            all = all.Where(s => s.Status == request.Status.Value).ToList();

        return all
            .OrderBy(s => s.LabTag)
            .Select(CreateStandardEquipmentCommandHandler.MapToDto)
            .ToList();
    }
}
