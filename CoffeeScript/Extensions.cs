using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoffeeScript
{
    static class Extensions
    {
        public static string Append(this string t, params string[] arguments)
        {
            var sb = new StringBuilder(t);
            foreach (var argument in arguments)
                sb.Append(argument);

            return sb.ToString();
        }

        public static string Unquote(this string t)
        {
            if (t.Length >= 2 && 
                ((t[0] == '"' && t[t.Length - 1] == '"') || 
                (t[0] == '\'' && t[t.Length - 1] == '\'')))
            {
                t = t.Substring(1, t.Length - 2);
                return t;
            }

            return t;
        }
    }
}
