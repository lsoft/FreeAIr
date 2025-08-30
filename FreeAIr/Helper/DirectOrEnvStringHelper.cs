namespace FreeAIr.Helper
{
    public static class DirectOrEnvStringHelper
    {
        public static string GetValue(
            string s
            )
        {
            if (s.StartsWith("{$") && s.EndsWith("}"))
            {
                //it's a env var!
                var varName = s.Substring(2, s.Length - 3);
                var result = Environment.GetEnvironmentVariable(varName);
                return result;
            }

            return s;
        }
    }
}
