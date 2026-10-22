using MediatR;
using TracLabs.Application.Features.StandardEquipment.Commands.CreateStandardEquipment;
using TracLabs.Application.Features.StandardEquipment.DTOs;
using TracLabs.Application.Interfaces.Repositories;

namespace TracLabs.Application.Features.StandardEquipment.Queries.GetExpiringStandards;

public record GetExpiringStandardsQuery(int WithinDays = 30) : IRequest<IReadOnlyList<StandardEquipmentDto>>;

public class GetExpiringStandardsQueryHandler : IRequestHandler<GetExpiringStandardsQuery, IReadOnlyList<StandardEquipmentDto>>
{
    private readonly IStandardEquipmentRepository _standardRepository;

    public GetExpiringStandardsQueryHandler(IStandardEquipmentRepository standardRepository)
    {
        _standardRepository = standardRepository;
    }

    public async Task<IReadOnlyList<StandardEquipmentDto>> Handle(GetExpiringStandardsQuery request, CancellationToken cancellationToken)
    {
        var standards = await _standardRepository.GetExpiringSoonAsync(request.WithinDays, cancellationToken);
        return standards
            .Select(CreateStandardEquipmentCommandHandler.MapToDto)
            .ToList();
    }
}
