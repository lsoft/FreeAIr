namespace FreeAIr.Helper
{
    public static  class LanguageHelper
    {
        /// <summary>
        /// Определяет префикс для разметки markdown на основе расширения файла.
        /// </summary>
        /// <param name="fileExtension">Расширение файла.</param>
        /// <returns>Название префикса для разметки markdown.</returns>
        /// 
        public static string GetMarkdownLanguageCodeBlockNameBasedOnFileExtension(
            string fileExtension
            )
        {
            if (string.Compare(fileExtension, ".sln", true) == 0)
            {
                return string.Empty;
            }
            if (string.Compare(fileExtension, ".slnx", true) == 0)
            {
                return "xml";
            }
            if (string.Compare(fileExtension, ".csproj", true) == 0)
            {
                return "xml";
            }
            if (string.Compare(fileExtension, ".slnf", true) == 0)
            {
                return "json";
            }
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
