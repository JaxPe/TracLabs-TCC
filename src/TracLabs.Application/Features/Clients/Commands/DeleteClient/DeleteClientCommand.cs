using MediatR;
using TracLabs.Application.Interfaces;
using TracLabs.Application.Interfaces.Repositories;

namespace TracLabs.Application.Features.Clients.Commands.DeleteClient;

public record DeleteClientCommand(Guid Id) : IRequest<bool>;

public class DeleteClientCommandHandler : IRequestHandler<DeleteClientCommand, bool>
{
    private readonly IClientRepository _clientRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteClientCommandHandler(IClientRepository clientRepository, IUnitOfWork unitOfWork)
    {
        _clientRepository = clientRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteClientCommand request, CancellationToken cancellationToken)
    {
        var client = await _clientRepository.GetByIdAsync(request.Id, cancellationToken);
        if (client == null)
            throw new KeyNotFoundException($"Cliente com ID '{request.Id}' não encontrado.");

        _clientRepository.SoftDelete(client);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
