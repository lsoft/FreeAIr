using FreeAIr.Helper;
using System.Text.RegularExpressions;

namespace FreeAIr.Find
{
    /// <summary>
    /// One entry of a `Find in Files` file mask, e.g. "*.cs" or "!*.g.cs", turned into the regex
    /// used to test file paths against it. A mask starting with "!" excludes matching files instead
    /// of including them.
    /// </summary>
    public sealed class FileTypeFilter
    {
        /// <summary>
        /// Whether this filter excludes matching files rather than including them, set when the
        /// original mask started with "!".
        /// </summary>
        public bool Exclude
        {
            get;
        }

        /// <summary>
        /// The original wildcard mask, with any leading "!" already stripped.
        /// </summary>
        public string WildcardFilter
        {
            get;
        }

        /// <summary>
        /// The regex compiled from <see cref="WildcardFilter"/>, used to test file paths.
        /// </summary>
        public Regex RegexFilter
        {
            get;
        }

        public FileTypeFilter(
            string filter
            )
        {
            if (filter is null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            filter = filter.Trim();
            if (filter.StartsWith("!"))
            {
                Exclude = true;
                filter = filter.Substring(1);
            }

            WildcardFilter = filter;
            RegexFilter = new Regex(
                filter.WildCardToRegular(),
                RegexOptions.IgnoreCase | RegexOptions.Singleline //not a compiled, this is NOT A STATIC regex! the content of this regex is different every time.
                );
        }

        /// <summary>
        /// Whether the given file path matches this filter's wildcard mask.
        /// </summary>
        public bool Match(string filePath)
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            return RegexFilter.IsMatch(filePath);
        }
    }


}
