namespace MarkdownParser.Helper
{
    public static class SubstringHelper
    {
        public static List<int> AllIndexesOf(
            this string subject,
            string substring,
            StringComparison comparison = StringComparison.Ordinal
            )
        {
            List<int> indexes = [];
            if (string.IsNullOrEmpty(substring))
                return indexes;

            var index = 0;
            while ((index = subject.IndexOf(substring, index, comparison)) != -1)
            {
                indexes.Add(index);
                index += substring.Length; // Move past the last found substring
            }
            return indexes;
        }
    }
}
