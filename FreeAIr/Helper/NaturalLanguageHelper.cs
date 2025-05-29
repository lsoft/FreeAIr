using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    public static class NaturalLanguageHelper
    {
        /// <summary>
        /// Удаляем символ ударения.
        /// </summary>
        public static string RemoveAcuteAccent(
            this string text
            )
        {
            return text.Replace("\u0301", "");
        }
    }
}
