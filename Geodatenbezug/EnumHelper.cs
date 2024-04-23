using System.ComponentModel;
namespace Geodatenbezug;

/// <summary>
/// Provides helper methods for <c>enum</c> handling.
/// </summary>
public static class EnumHelper
{
    /// <summary>
    /// Retrieves the description of the given <c>enum</c> value. If the <c>enum</c> value does not have a <c>DescriptionAttribute</c>, the <c>enum</c> value itself is returned.
    /// </summary>
    public static string GetDescription(this Enum value)
    {
        var fieldInfo = value.GetType().GetField(value.ToString());
        if (fieldInfo != null)
        {
            var attributes = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];

            if (attributes != null && attributes.Length > 0)
            {
                return attributes[0].Description;
            }
        }

        return value.ToString();
    }
}
