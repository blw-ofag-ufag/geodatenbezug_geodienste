using System.Text.RegularExpressions;

namespace Geodatenbezug;

/// <summary>
/// Provides helper methods.
/// </summary>
public static class Helper
{
    /// <summary>
    /// Extracts a setting from a settings string by key.
    /// </summary>
    public static string ExtractSettingByKey(string settings, string key)
    {
        var pattern = key + @"=(?<setting>[^;]+)";
        var match = Regex.Match(settings, pattern);
        if (match.Success)
        {
            return match.Groups["setting"].Value;
        }
        else
        {
            throw new KeyNotFoundException($"Kein Setting für Key {key} gefunden.");
        }
    }
}
