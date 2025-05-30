using System.Linq;
using System.Text.RegularExpressions;

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
