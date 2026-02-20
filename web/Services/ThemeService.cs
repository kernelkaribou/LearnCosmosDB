using Microsoft.JSInterop;

namespace LearnCosmosDB.Web.Services;

/// <summary>
/// Manages dark/light theme state with localStorage persistence.
/// </summary>
public class ThemeService
{
    private readonly IJSRuntime _js;
    private bool _isDark;

    public bool IsDark => _isDark;
    public event Action? OnChange;

    public ThemeService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task InitializeAsync()
    {
        try
        {
            var stored = await _js.InvokeAsync<string?>("localStorage.getItem", "theme");
            _isDark = stored == "dark";
            await ApplyTheme();
        }
        catch
        {
            // SSR or prerendering — ignore
        }
    }

    public async Task ToggleAsync()
    {
        _isDark = !_isDark;
        await ApplyTheme();
        await _js.InvokeVoidAsync("localStorage.setItem", "theme", _isDark ? "dark" : "light");
        OnChange?.Invoke();
    }

    private async Task ApplyTheme()
    {
        await _js.InvokeVoidAsync("eval", $"document.documentElement.setAttribute('data-theme', '{(_isDark ? "dark" : "light")}')");
    }
}
