using System;

namespace FreeAIr.SetupWizard.Helper
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

        /// <summary>
        /// Whether <paramref name="s"/> is written in the <c>{$VAR_NAME}</c> environment-variable
        /// reference form, as opposed to holding a literal value.
        /// </summary>
        public static bool IsEnvReference(
            string s
            )
        {
            return !string.IsNullOrEmpty(s) && s.StartsWith("{$") && s.EndsWith("}") && s.Length >= 3;
        }

        /// <summary>
        /// Builds the <c>{$VAR_NAME}</c> form referencing the given environment variable name.
        /// </summary>
        public static string MakeEnvReference(
            string varName
            )
        {
            return "{$" + varName + "}";
        }

        /// <summary>
        /// Extracts the environment variable name out of a <c>{$VAR_NAME}</c> reference, or returns
        /// <see langword="false"/> when <paramref name="s"/> is not in that form.
        /// </summary>
        public static bool TryGetVarName(
            string s,
            out string varName
            )
        {
            if (!IsEnvReference(s))
            {
                varName = string.Empty;
                return false;
            }

            varName = s.Substring(2, s.Length - 3);
            return true;
        }
    }
}
