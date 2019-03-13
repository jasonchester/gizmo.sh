using System;

namespace Gizmo
{
    public static class StringExtensions
    {
        public static bool IsNotQuit(this string s)
        {
            if(s == null) return true;
            s = s.Trim().ToLower();
            return !(s == ":q" || s == ":quit");
        }

        public static bool IsNullOrEmpty(this string val)
        {
            return string.IsNullOrEmpty(val);
        }

        public static string Truncate(this string text, int length, string ellipsis = null, bool breakOnWord = false)
        {
            if (text.IsNullOrEmpty()) return string.Empty;
            if (text.Length < length) return text;

            if(ellipsis.IsNullOrEmpty())
            {
                text = text.Substring(0, length - ellipsis.Length);
            }
            else
            {
                text = text.Substring(0, length);
            }

            if (breakOnWord)
            {
                text = text.Substring(0, text.LastIndexOf(' '));
            }

            return text + ellipsis;
        }
    }
}
