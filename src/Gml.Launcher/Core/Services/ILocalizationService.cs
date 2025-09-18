using System.Globalization;

namespace Gml.Launcher.Core.Services;

/// <summary>
///     Represents the interface for localization services.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    ///     Retrieves a localized string for the specified key.
    /// </summary>
    /// <param name="key">The key of the localized string.</param>
    /// <returns>The localized string.</returns>
    string GetString(string key);
    
    /// <summary>
    ///     Changes the current language/culture.
    /// </summary>
    /// <param name="culture">The culture to change to.</param>
    void ChangeLanguage(CultureInfo culture);
}
