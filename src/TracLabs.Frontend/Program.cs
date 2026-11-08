using TracLabs.Frontend.Components;
using TracLabs.Frontend.Services;
using TracLabs.Frontend.Services.Auth;
using TracLabs.Frontend.Services.Notification;
using TracLabs.Frontend.Services.Theme;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure HttpClient for TracLabs Backend REST API
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5259";
builder.Services.AddHttpClient("TracLabsApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("TracLabsApi"));

// Metrology & Frontend Core Services
builder.Services.AddScoped<IThemeService, ThemeService>();
builder.Services.AddScoped<IUserContextService, UserContextService>();
builder.Services.AddScoped<INotificationModalService, NotificationModalService>();
builder.Services.AddScoped<IMetrologyDataService, HttpMetrologyDataService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
