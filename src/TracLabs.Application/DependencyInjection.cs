using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace TracLabs.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // MediatR — registra todos os IRequestHandler<> do assembly
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // AutoMapper v14 — a API mudou, AddAutoMapper agora recebe o assembly diretamente
        services.AddAutoMapper(cfg => cfg.AddMaps(assembly));

        // FluentValidation — no .NET 10 + FV 12 está no pacote principal
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}