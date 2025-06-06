using FreeAIr.Shared.Helper;
using System.Collections.Generic;
using System.Linq;

namespace FreeAIr.Find
{
    public sealed class FileTypesFilter
    {
        public IReadOnlyList<FileTypeFilter> IncludeFilters
        {
            get;
        }

        public IReadOnlyList<FileTypeFilter> ExcludeFilters
        {
            get;
        }

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
