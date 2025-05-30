namespace FreeAIr.Helper
{
    public static class StringHelper
    {
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
