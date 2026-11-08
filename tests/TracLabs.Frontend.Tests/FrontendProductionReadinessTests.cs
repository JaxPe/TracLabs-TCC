using FluentAssertions;
using Microsoft.JSInterop;
using TracLabs.Frontend.Models.Enums;
using TracLabs.Frontend.Services.Auth;
using TracLabs.Frontend.Services.Theme;
using Xunit;

namespace TracLabs.Frontend.Tests;

public class FrontendProductionReadinessTests
{
    private class DummyJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => ValueTask.FromResult(default(TValue)!);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => ValueTask.FromResult(default(TValue)!);
    }

    private readonly IJSRuntime _jsRuntime = new DummyJsRuntime();

    [Fact]
    public async Task ThemeService_Should_Default_To_PrecisionSlate_Style()
    {
        var themeService = new ThemeService(_jsRuntime);

        await themeService.InitializeAsync();

        themeService.CurrentStyle.Should().Be(VisualStyle.PrecisionSlate);
        themeService.CurrentTheme.Should().Be(ThemeMode.Light);

        await themeService.ToggleThemeModeAsync();
        themeService.CurrentTheme.Should().Be(ThemeMode.Dark);

        await themeService.ToggleVisualStyleAsync();
        themeService.CurrentStyle.Should().Be(VisualStyle.PrecisionSlate);
    }

    [Fact]
    public void CertificateStatus_Enum_Must_Match_Backend_Values()
    {
        ((int)CertificateStatus.Draft).Should().Be(0);
        ((int)CertificateStatus.Issued).Should().Be(1);
        ((int)CertificateStatus.Superseded).Should().Be(2);
    }

    [Fact]
    public async Task UserContextService_Logout_Should_Reset_State_And_Clear_Session()
    {
        var userContext = new UserContextService(null, _jsRuntime);

        userContext.SetRole(UserRole.Tecnico);
        userContext.CurrentUser.Role.Should().Be(UserRole.Tecnico);

        await userContext.LogoutAsync();

        userContext.IsAuthenticated.Should().BeFalse();
        userContext.Token.Should().BeNull();
    }

    [Fact]
    public void UserProfile_Should_Have_Correct_Initials_And_Description()
    {
        var profile = new UserProfile(
            "u1", "Carlos Eduardo Silva", "carlos@traclabs.com",
            UserRole.Tecnico, "CS", "CFT/CRT 198420-SP", "Técnico Metrologista"
        );

        profile.RoleDescription.Should().Be("Técnico Metrologista Executor");
        profile.AvatarInitials.Should().Be("CS");

        var supervisorProfile = new UserProfile(
            "u2", "Dr. Alexandre Morales", "alexandre@traclabs.com",
            UserRole.Supervisor, "AM", "CRQ IV 04218901", "Supervisor Metrológico"
        );

        supervisorProfile.RoleDescription.Should().Be("Supervisor Técnico Metrológico");
    }

    [Fact]
    public void Unauthenticated_State_Should_Have_IsAuthenticated_False()
    {
        var userContext = new UserContextService(null, _jsRuntime);
        userContext.IsAuthenticated.Should().BeFalse();
        userContext.Token.Should().BeNull();
    }
}