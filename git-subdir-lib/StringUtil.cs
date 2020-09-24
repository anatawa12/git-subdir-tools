using System;

namespace GitSubdirTools.Libs
{
    public static class StringUtil
    {
        public static bool IsValidObjectId(this string str)
        {
            if (str.Length != 40)
                return false;
            foreach (var c in str)
            {
                if (!('0' <= c && c <= '9' || 'a' <= c && c <= 'f' || 'A' <= c && c <= 'F'))
                    return false;
            }

            return true;
        }

        public static string RemoveSuffix(this string str, string suffix)
        {
            return str.EndsWith(suffix) ? str[..^suffix.Length] : str;
        }

        public static string RemovePrefix(this string str, string prefix)
        {
            return str.StartsWith(prefix) ? str[prefix.Length..] : str;
        }

        public static bool IsValidObjectId(this ReadOnlySpan<char> str)
        {
            if (str.Length != 40)
                return false;
            foreach (var c in str)
            {
                if (!('0' <= c && c <= '9' || 'a' <= c && c <= 'f' || 'A' <= c && c <= 'F'))
                    return false;
            }

            return true;
        }

        public static ReadOnlySpan<char> RemoveSuffix(this ReadOnlySpan<char> str, ReadOnlySpan<char> suffix)
        {
            return str.EndsWith(suffix) ? str[..^suffix.Length] : str;
        }

        public static ReadOnlySpan<char> RemovePrefix(this ReadOnlySpan<char> str, ReadOnlySpan<char> prefix)
        {
            return str.StartsWith(prefix) ? str[prefix.Length..] : str;
        }
    }
}
