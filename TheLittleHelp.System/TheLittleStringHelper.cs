using System;
using System.Security;

namespace TheLittleHelp.System.StringHelp
{
    public static class TheLittleStringHelper
    {
        public static bool IsNullOrEmpty(this string s)
        {
            return String.IsNullOrEmpty(s);
        }
        public static bool IsFilled(this string s)
        {
            return !String.IsNullOrEmpty(s);
        }
        public static string DefaultIfEmpty(this string s, string @default) => string.IsNullOrEmpty(s) ? @default : s;
        public static bool AllFilled(params string[] arr)
        {
            foreach (string s in arr)
            {
                if (!s.IsFilled()) return false;
            }
            return true;
        }

        public static bool AnyFilled(params string[] arr)
        {
            foreach (string s in arr)
            {
                if (s.IsFilled()) return true;
            }
            return false;
        }
        public static SecureString ToSecureString(this string value)
        {
            var ss = new SecureString();
            var len = value.Length;
            for (int i = 0; i < len; i++)
                ss.AppendChar(value[i]);
            return ss;
        }
    }
}
