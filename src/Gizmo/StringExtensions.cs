namespace Gizmo
{
    public static class StringExtensions
    {
        public static bool IsNotQuit(this string s)
        {
            if(s == null) return true;
            s = s.Trim().ToLower();
            return s == ":q" || s == ":quit";
        }
    }
}
