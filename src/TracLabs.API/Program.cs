using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using TracLabs.API.Middleware;
using TracLabs.API.Services;
using TracLabs.Application;
using TracLabs.Application.Constants;
using TracLabs.Application.Interfaces;
using TracLabs.Infrastructure;
using TracLabs.Infrastructure.Identity;
using TracLabs.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();

// ── Swagger com suporte a JWT ─────────────────────────────────────────────────
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TracLabs API",
        Version = "v1",
        Description = "API para gestão de equipamentos e emissão de certificados em laboratórios de metrologia."
    });

    // Adiciona o campo de token JWT na interface do Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe o token JWT. Exemplo: Bearer {seu_token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ── CORS (necessário quando o Blazor rodar em porta diferente) ────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("TracLabsPolicy", policy =>
    {
        policy
            .WithOrigins("https://localhost:5173", "http://localhost:5173", "https://localhost:7173")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// ── Middleware pipeline ────────────────────────────────────────────────────────
// A ordem aqui importa — middleware é executado na sequência em que é registrado.

app.UseMiddleware<ExceptionHandlingMiddleware>(); // Primeiro — captura tudo

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("TracLabsPolicy");
app.UseHttpsRedirection();
app.UseAuthentication(); // Antes do Authorization
app.UseAuthorization();
app.MapControllers();

// ── Seed de roles e usuários padrão no startup ───────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TracLabsDbContext>();
    await dbContext.Database.MigrateAsync();

    // Sincroniza o papel metrológico inicial dos padrões existentes no banco caso estejam como Balança
    var existingStandards = await dbContext.StandardEquipment.ToListAsync();
    bool hasUpdatedStandards = false;
    foreach (var std in existingStandards)
    {
        var text = $"{std.InstrumentType} {std.Name}".ToLowerInvariant();
        var inferred = TracLabs.Domain.Enums.StandardRole.Balance;
        if (text.Contains("balan")) inferred = TracLabs.Domain.Enums.StandardRole.Balance;
        else if (text.Contains("peso") || text.Contains("massa")) inferred = TracLabs.Domain.Enums.StandardRole.ReferenceWeights;
        else if (text.Contains("termo") && (text.Contains("liq") || text.Contains("imers") || text.Contains("agua") || text.Contains("água")))
            inferred = TracLabs.Domain.Enums.StandardRole.LiquidThermometer;
        else if (text.Contains("termohigro") || text.Contains("termo-higro"))
            inferred = TracLabs.Domain.Enums.StandardRole.ThermoHygrometer;
        else if (text.Contains("termo") && (text.Contains("amb") || text.Contains("sala")))
            inferred = TracLabs.Domain.Enums.StandardRole.AmbientThermometer;
        else if (text.Contains("higro")) inferred = TracLabs.Domain.Enums.StandardRole.Hygrometer;
        else if (text.Contains("baro") || text.Contains("press")) inferred = TracLabs.Domain.Enums.StandardRole.Barometer;
        else if (text.Contains("picno")) inferred = TracLabs.Domain.Enums.StandardRole.Pycnometer;
        else if (text.Contains("densim")) inferred = TracLabs.Domain.Enums.StandardRole.Other;

        if (std.Role == TracLabs.Domain.Enums.StandardRole.Balance && inferred != TracLabs.Domain.Enums.StandardRole.Balance)
        {
            std.UpdateRole(inferred);
            hasUpdatedStandards = true;
        }
    }
    if (hasUpdatedStandards)
    {
        await dbContext.SaveChangesAsync();
    }

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    foreach (var role in new[] { Roles.Tecnico, Roles.Supervisor })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    var supervisorEmail = "supervisor@traclabs.com";
    if (await userManager.FindByEmailAsync(supervisorEmail) == null)
    {
        var supervisor = new ApplicationUser
        {
            UserName = supervisorEmail,
            Email = supervisorEmail,
            FullName = "Dr. Alexandre Morales",
            ProfessionalRegistration = "CRQ IV 04218901",
            EmailConfirmed = true,
            IsActive = true
        };
        await userManager.CreateAsync(supervisor, "Password123!");
        await userManager.AddToRoleAsync(supervisor, Roles.Supervisor);
    }

    var tecnicoEmail = "tecnico@traclabs.com";
    if (await userManager.FindByEmailAsync(tecnicoEmail) == null)
    {
        var tecnico = new ApplicationUser
        {
            UserName = tecnicoEmail,
            Email = tecnicoEmail,
            FullName = "Carlos Eduardo Silva",
            ProfessionalRegistration = "CFT/CRT 198420-SP",
            EmailConfirmed = true,
            IsActive = true
        };
        await userManager.CreateAsync(tecnico, "Password123!");
        await userManager.AddToRoleAsync(tecnico, Roles.Tecnico);
    }
}

app.Run();
