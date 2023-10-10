using System.Diagnostics.CodeAnalysis;

namespace SerializableDictionaryPlugin;

internal class DeterministicStringEqualityComparer : EqualityComparer<string>
{
    public static DeterministicStringEqualityComparer Instance { get; } = new();

    public override bool Equals(string? x, string? y)
    {
        return Default.Equals(x, y);
    }

    /// <summary>
    /// Legacy implementation of GetHashCode that produces a constant value between program runs.
    /// </summary>
    /// <remarks>
    /// Sourced from <seealso href="https://github.com/mono/mono/blob/ea8a24b1bbf950699336bd56e9bab9f046da11c5/mcs/class/referencesource/mscorlib/system/string.cs#L801"/>
    /// </remarks>
    /// <param name="obj">Input string.</param>
    /// <returns>The hash code.</returns>
    public override int GetHashCode(string obj)
    {
        unsafe
        {
            fixed (char* src = obj)
            {
                int hash1 = 5381;
                int hash2 = hash1;

                int c;
                char* s = src;
                while ((c = s[0]) != 0)
                {
                    hash1 = (hash1 << 5) + hash1 ^ c;
                    c = s[1];
                    if (c == 0)
                        break;
                    hash2 = (hash2 << 5) + hash2 ^ c;
                    s += 2;
                }

                return hash1 + hash2 * 1566083941;
            }
        }
    }
}
