using System.Text.RegularExpressions;

namespace Geodatenbezug;

/// <summary>
/// Provides helper methods for <c>string</c> handling.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Extracts a value for the given <c>string</c> input.
    /// </summary>
    public static string ExtractValueByKey(this string input, string key)
    {
        var pattern = key + @"=(?<value>[^;]+)";
        var match = Regex.Match(input, pattern);
        if (match.Success)
        {
            return match.Groups["value"].Value;
        }
        else
        {
            throw new KeyNotFoundException($"Kein Wert für Key {key} gefunden.");
        }
    }
}
