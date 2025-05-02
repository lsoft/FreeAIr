using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    public static  class LanguageHelper
    {
        public static string GetMarkdownLanguageCodeBlockNameBasedOnFileExtension(
            string fileExtension
            )
        {
            if (string.Compare(fileExtension, ".cs", true) == 0)
            {
                return "csharp";
            }
            if (string.Compare(fileExtension, ".xaml", true) == 0)
            {
                return "xml";
            }
            if (string.Compare(fileExtension, ".xml", true) == 0)
            {
                return "xml";
            }
            if (string.Compare(fileExtension, ".html", true) == 0)
            {
                return "html";
            }
            if (string.Compare(fileExtension, ".aspx", true) == 0)
            {
                return "html";
            }
            if (string.Compare(fileExtension, ".json", true) == 0)
            {
                return "json";
            }
            if (string.Compare(fileExtension, ".cpp", true) == 0)
            {
                return "cpp";
            }

            return string.Empty;
        }
    }
}
