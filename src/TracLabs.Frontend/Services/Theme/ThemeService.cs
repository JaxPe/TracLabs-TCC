using Microsoft.JSInterop;

namespace TracLabs.Frontend.Services.Theme;

public sealed class ThemeService : IThemeService
{
    private readonly IJSRuntime _jsRuntime;
    private bool _initialized;

    public ThemeMode CurrentTheme { get; private set; } = ThemeMode.Light;
    public VisualStyle CurrentStyle => VisualStyle.PrecisionSlate;

    public event Action? OnThemeChanged;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        try
        {
            var themeStr = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "traclabs_theme");

            if (Enum.TryParse<ThemeMode>(themeStr, true, out var parsedTheme))
            {
                CurrentTheme = parsedTheme;
            }

            await ApplyAttributesAsync();
            _initialized = true;
            OnThemeChanged?.Invoke();
        }
        catch (InvalidOperationException)
        {
            // Prerender safe fallback
        }
    }

    public async Task SetThemeModeAsync(ThemeMode mode)
    {
        CurrentTheme = mode;
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "traclabs_theme", mode.ToString().ToLowerInvariant());
            await ApplyAttributesAsync();
        }
        catch (InvalidOperationException) { }

        OnThemeChanged?.Invoke();
    }

    public async Task ToggleThemeModeAsync()
    {
        var nextMode = CurrentTheme == ThemeMode.Light ? ThemeMode.Dark : ThemeMode.Light;
        await SetThemeModeAsync(nextMode);
    }

    public async Task SetVisualStyleAsync(VisualStyle style)
    {
        await ApplyAttributesAsync();
        OnThemeChanged?.Invoke();
    }

    public async Task ToggleVisualStyleAsync()
    {
        await ApplyAttributesAsync();
        OnThemeChanged?.Invoke();
    }

    private async Task ApplyAttributesAsync()
    {
        var themeValue = CurrentTheme == ThemeMode.Dark ? "dark" : "light";

        try
        {
            await _jsRuntime.InvokeVoidAsync("traclabsTheme.setThemeAttributes", themeValue, "precision-slate");
        }
        catch (InvalidOperationException) { }
    }
}
