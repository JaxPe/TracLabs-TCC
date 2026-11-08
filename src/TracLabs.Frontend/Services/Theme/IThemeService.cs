namespace TracLabs.Frontend.Services.Theme;

public enum ThemeMode
{
    Light,
    Dark
}

public enum VisualStyle
{
    PrecisionSlate,
    MetrologyClean
}

public interface IThemeService
{
    ThemeMode CurrentTheme { get; }
    VisualStyle CurrentStyle { get; }
    event Action? OnThemeChanged;

    Task InitializeAsync();
    Task SetThemeModeAsync(ThemeMode mode);
    Task ToggleThemeModeAsync();
    Task SetVisualStyleAsync(VisualStyle style);
    Task ToggleVisualStyleAsync();
}
