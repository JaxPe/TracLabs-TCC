using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;
using TracLabs.API.Middleware;
using TracLabs.API.Services;
using TracLabs.Application;
using TracLabs.Application.Interfaces;
using TracLabs.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddControllers();
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
            .WithOrigins("https://localhost:5173", "http://localhost:5173")
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

// ── Seed de roles no startup ──────────────────────────────────────────────────
// Garante que as roles existam no banco toda vez que a API sobe,
// sem precisar rodar um script manual.
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    foreach (var role in new[] { "Tecnico", "Supervisor" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}

app.Run();
