namespace FreeAIr.Helper
{
    /// <summary>
    /// Resolves a settings string that may either hold a literal value or, written as
    /// <c>{$VAR_NAME}</c>, a reference to an environment variable. Lets settings such as an API token
    /// be kept out of the settings file itself and supplied through the environment instead.
    /// </summary>
    public static class DirectOrEnvStringHelper
    {
        /// <summary>
        /// Returns the environment variable's value when <paramref name="s"/> has the
        /// <c>{$VAR_NAME}</c> form, otherwise returns <paramref name="s"/> unchanged.
        /// </summary>
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
