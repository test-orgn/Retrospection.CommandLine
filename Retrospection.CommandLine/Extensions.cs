
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("UnitTests")]


namespace Retrospection.CommandLine
{
    internal static class Extensions
    {
        public static void AddRange<TKey, TValue>(this Dictionary<TKey, TValue> dict, IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            foreach (var item in items)
            {
                dict.Add(item.Key, item.Value);
            }
        }
        public static bool ContainsUnescaped(this string expression, char value)
        {
            if (!expression.Contains(value))
            {
                return false;
            }
            else
            {
                var ndx = expression.IndexOf(value);

                while (ndx != -1)
                {
                    // If it's not the first char in the string, and the char before it isn't \, then it is in the string in an unescaped form.
                    if (ndx > 0 && expression[ndx - 1] != '\\')
                    {
                        return true;
                    }
                    else // keep looking
                    {
                        ndx = expression.IndexOf(value, ndx + 1);
                    }
                }
                return false;
            }
        }
        public static bool EndsWithUnescaped(this string expression, char value)
        {
            if (!expression.EndsWith(value))
            {
                return false;
            }
            else
            {
                return expression.Length > 1 && expression[expression.Length - 2] != '\\';
            }
        }
    }
}