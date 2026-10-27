using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TracLabs.Application.Common.Behaviors;

namespace TracLabs.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // MediatR — registra todos os IRequestHandler<> e o pipeline behavior de validação
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        // AutoMapper — registra perfis do assembly
        services.AddAutoMapper(cfg => cfg.AddMaps(assembly));

        // FluentValidation — registra todos os AbstractValidator<> do assembly
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
