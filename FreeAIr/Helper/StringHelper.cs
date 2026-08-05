namespace FreeAIr.Helper
{
    /// <summary>
    /// Small general-purpose string manipulation helpers used across FreeAIr.
    /// </summary>
    public static class StringHelper
    {
        /// <summary>
        /// Returns a new string with the characters of <paramref name="s"/> in reverse order.
        /// </summary>
        public static string ReverseString(
            this string s
            )
        {
            var charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
    }
}
