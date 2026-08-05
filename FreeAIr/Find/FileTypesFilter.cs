using FreeAIr.Shared.Helper;
using System.Collections.Generic;
using System.Linq;

namespace FreeAIr.Find
{
    /// <summary>
    /// The full file mask of a `Find in Files` search, split into the include and exclude filters
    /// parsed out of it. A file passes when it matches at least one include filter (or there are
    /// none) and no exclude filter.
    /// </summary>
    public sealed class FileTypesFilter
    {
        /// <summary>
        /// The masks a file must match at least one of, to be searched.
        /// </summary>
        public IReadOnlyList<FileTypeFilter> IncludeFilters
        {
            get;
        }

        /// <summary>
        /// The masks that veto a file even when it matches an include filter.
        /// </summary>
        public IReadOnlyList<FileTypeFilter> ExcludeFilters
        {
            get;
        }

        /// <summary>Splits the given masks into include and exclude filters by each one's <c>Exclude</c> flag.</summary>
        public FileTypesFilter(
            IReadOnlyList<FileTypeFilter> filters
            )
        {
            if (filters is null)
            {
                throw new ArgumentNullException(nameof(filters));
            }

            IncludeFilters = filters.FindAll(f => !f.Exclude);
            ExcludeFilters = filters.FindAll(f => f.Exclude);
        }

        /// <summary>
        /// Whether the given file path should be searched under this combined include/exclude mask.
        /// </summary>
        public bool Match(string filePath)
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (IncludeFilters.Count == 0 && ExcludeFilters.Count == 0)
            {
                return true;
            }

            if (ExcludeFilters.Count > 0 && ExcludeFilters.Any(f => f.Match(filePath)))
            {
                return false;
            }
            if (IncludeFilters.Count == 0 || IncludeFilters.Any(f => f.Match(filePath)))
            {
                return true;
            }

            return false;
        }
    }


}
