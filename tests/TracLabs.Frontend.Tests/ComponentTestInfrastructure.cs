using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace TracLabs.Frontend.Tests;

/// <summary>
/// Lightweight navigation manager for testing Blazor component routing.
/// </summary>
public class TestNavigationManager : NavigationManager
{
    public TestNavigationManager()
    {
        Initialize("http://localhost/", "http://localhost/");
    }

    public string? LastNavigatedUri { get; private set; }
    public List<string> NavigationHistory { get; } = new();

    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        LastNavigatedUri = uri;
        NavigationHistory.Add(uri);
        Uri = ToAbsoluteUri(uri).ToString();
    }
}

/// <summary>
/// Mock JS runtime capturing invocations for assertion.
/// </summary>
public class TestJsRuntime : IJSRuntime
{
    public List<(string Identifier, object?[]? Args)> Invocations { get; } = new();

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        Invocations.Add((identifier, args));
        return ValueTask.FromResult(default(TValue)!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        Invocations.Add((identifier, args));
        return ValueTask.FromResult(default(TValue)!);
    }

    public bool HasInvoked(string identifier) => Invocations.Any(i => i.Identifier == identifier);

    public object?[]? GetInvocationArgs(string identifier) =>
        Invocations.FirstOrDefault(i => i.Identifier == identifier).Args;
}

/// <summary>
/// Reflection helpers for testing Blazor components without render pipeline side-effects.
/// </summary>
public static class ComponentTestHarness
{
    public static void InitializeComponent(ComponentBase component)
    {
        // Mark pending queued render as true so ComponentBase.StateHasChanged() immediately returns
        // without calling the unattached render handle.
        var field = typeof(ComponentBase).GetField("_hasPendingQueuedRender", BindingFlags.Instance | BindingFlags.NonPublic);
        field?.SetValue(component, true);

        var modalProp = component.GetType().GetProperty("NotificationModal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (modalProp != null && modalProp.GetValue(component) == null)
        {
            modalProp.SetValue(component, new Services.Notification.NotificationModalService());
        }
    }

    public static void SetProperty(object target, string propertyName, object? value)
    {
        var prop = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        prop?.SetValue(target, value);
    }

    public static T? GetProperty<T>(object target, string propertyName)
    {
        var prop = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return (T?)prop?.GetValue(target);
    }

    public static void SetField(object target, string fieldName, object? value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        field?.SetValue(target, value);
    }

    public static T? GetField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return (T?)field?.GetValue(target);
    }

    public static async Task InvokeAsync(object target, string methodName, params object?[]? args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (method == null) throw new MissingMethodException(target.GetType().FullName, methodName);
        var result = method.Invoke(target, args);
        if (result is Task t)
        {
            await t;
        }
    }

    public static T? Invoke<T>(object target, string methodName, params object?[]? args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (method == null) throw new MissingMethodException(target.GetType().FullName, methodName);
        return (T?)method.Invoke(target, args);
    }

    public static void Invoke(object target, string methodName, params object?[]? args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (method == null) throw new MissingMethodException(target.GetType().FullName, methodName);
        method.Invoke(target, args);
    }
}
