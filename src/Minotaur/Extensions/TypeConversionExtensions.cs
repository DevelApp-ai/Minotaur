using Minotaur.Core;

namespace Minotaur.Extensions;

/// <summary>
/// Extension methods for type conversions and convenience operations on GSSM types
/// </summary>
public static class TypeConversionExtensions
{
    /// <summary>
    /// Converts a list to an array
    /// </summary>
    public static T[] ToArray<T>(this List<T> list) => list.ToArray();

    /// <summary>
    /// Converts an array to a list
    /// </summary>
    public static List<T> ToList<T>(this T[] array) => new List<T>(array);
}
