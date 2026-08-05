using FreeAIr.Options2;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    /// <summary>
    /// Helper for looking up strings from the FreeAIr resources (.resx) using the answer language
    /// culture configured in the extension options, so UI and prompt text can be localized.
    /// </summary>
    public static class ResourceHelper
    {
        /// <summary>
        /// Looks up a localized string by resource name, using the culture the user configured
        /// for answers, and returns it (or <c>null</c> if the resource name is not found).
        /// </summary>
        public static async Task<string> GetLocalizedResourceByNameAsync(
            this string resourceName
            )
        {
            var unsorted = await FreeAIrOptions.DeserializeUnsortedAsync();

            return FreeAIr.Resources.Resources.ResourceManager.GetString(
                resourceName,
                unsorted.GetAnswerCulture()
                );
        }
    }
}
