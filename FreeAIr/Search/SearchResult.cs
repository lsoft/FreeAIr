using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.Seaarch
{
    /// <summary>
    /// Результат поиска.
    /// </summary>
    public sealed class SearchResult
    {
        /// <summary>
        /// Заголовок пункта из выдачи поисковой системы.
        /// </summary>
        public string Title
        {
            get;
        }

        /// <summary>
        /// Описание пункта из выдачи поисковой системы.
        /// </summary>
        public string Details
        {
            get;
        }


        /// <summary>
        /// Ссылка на источник пункта из выдачи поисковой системы.
        /// </summary>
        public string Link
        {
            get;
        }

        public SearchResult(string title, string details, string link)
        {
            Title = title;
            Details = details;
            Link = link;
        }
    }
}
