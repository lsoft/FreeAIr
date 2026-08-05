namespace FreeAIr.Helper
{
    /// <summary>
    /// String helpers for cleaning up natural language text before it is shown to the user
    /// or sent to a model, such as stripping diacritics left over from speech recognition.
    /// </summary>
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
