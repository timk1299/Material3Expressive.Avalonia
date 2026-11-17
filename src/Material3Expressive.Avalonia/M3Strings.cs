using Avalonia;
using Avalonia.Controls;

namespace Material3Expressive.Avalonia;

/// <summary>
/// Looks up the texts of the theme (the resources <c>M3String…</c> of <c>Resources/Strings.axaml</c>),
/// which an app replaces in its own resources to localize them.
/// </summary>
internal static class M3Strings
{
    /// <summary>
    /// The text for <paramref name="key"/> as seen from <paramref name="scope"/>; from the
    /// application when the scope is not in a tree yet.
    /// </summary>
    public static string Get(StyledElement? scope, string key, string fallback)
    {
        if (scope != null && scope.TryFindResource(key, out var value) && value is string text)
            return text;
        if (Application.Current is { } app && app.TryFindResource(key, out value) && value is string appText)
            return appText;
        return fallback;
    }

    /// <summary>As <see cref="Get"/>, with <paramref name="argument"/> in place of <c>{0}</c>.</summary>
    public static string Format(StyledElement? scope, string key, string fallback, object? argument)
    {
        var format = Get(scope, key, fallback);
        try
        {
            return string.Format(System.Globalization.CultureInfo.CurrentCulture, format, argument);
        }
        catch (System.FormatException)
        {
            // A replaced text with broken braces is shown as it is.
            return format;
        }
    }
}
