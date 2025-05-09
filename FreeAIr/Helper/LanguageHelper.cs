using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    public static  class LanguageHelper
    {
        /// <summary>
        /// Определяет язык программирования на основе расширения файла.
        /// </summary>
        /// <param name="fileExtension">Расширение файла.</param>
        /// <returns>Название языка программирования в формате Markdown.</returns>
        /// 
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
            if (string.Compare(fileExtension, ".js", true) == 0)
            {
                return "javascript";
            }
            if (string.Compare(fileExtension, ".py", true) == 0)
            {
                return "python";
            }
            if (string.Compare(fileExtension, ".rb", true) == 0)
            {
                return "ruby";
            }

            return string.Empty;
        }
    }
}
