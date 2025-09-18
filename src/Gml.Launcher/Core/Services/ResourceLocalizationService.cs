using System;
using System.Globalization;
using Avalonia;
using Gml.Launcher.Assets.Resources;

namespace Gml.Launcher.Core.Services;

public class ResourceLocalizationService : ILocalizationService
{
    public string GetString(string key)
    {
        if (Application.Current == null) throw new ArgumentException($"Key '{key}' not found in resources");

        return TryGetLocalizedString(key) ?? throw new Exception($"Resource \"{key}\" not found");
    }

    private static string? TryGetLocalizedString(string key)
    {
        return Resources.ResourceManager.GetString(key, Resources.Culture);
    }
    
    public void ChangeLanguage(CultureInfo culture)
    {
        if (Application.Current != null)
        {
            Resources.Culture = culture;
        }
    }
}
