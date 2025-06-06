using FreeAIr.Helper;
using System.Text.RegularExpressions;

namespace FreeAIr.Find
{
    public sealed class FileTypeFilter
    {
        public bool Exclude
        {
            get;
        }

        public string WildcardFilter
        {
            get;
        }

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
