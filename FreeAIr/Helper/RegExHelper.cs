using System.Linq;
using System.Text.RegularExpressions;

namespace FreeAIr.Helper
{
    /// <summary>
    /// Helpers for working with simple file-glob wildcard patterns (<c>?</c> and <c>*</c>), such as
    /// those used for include/exclude filters, converting them to and validating them against regex.
    /// </summary>
    public static class RegExHelper
    {
        /// <summary>
        /// Checks whether the wildcard pattern contains at least one literal character besides
        /// <c>?</c> and <c>*</c>, guarding against patterns that would match everything.
        /// </summary>
        public static bool IsCorrectWildcard(
            this string value
            )
        {
            return
                value.ToCharArray().Any(j => j != '?' && j != '*');
        }

        /// <summary>
        /// Converts a simple <c>?</c>/<c>*</c> wildcard pattern (e.g. a file glob) into an anchored
        /// regular expression string that matches the same set of values.
        /// </summary>
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
