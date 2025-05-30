using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    public static class RegExHelper
    {
        public static bool IsCorrectWildcard(
            this string value
            )
        {
            return
                value.ToCharArray().Any(j => j != '?' && j != '*');
        }

        public static string WildCardToRegular(
            this string value
            )
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return
                "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
        }

    }
}
