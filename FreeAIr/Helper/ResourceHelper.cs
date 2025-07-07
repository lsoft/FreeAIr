using FreeAIr.Options2;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    public static class ResourceHelper
    {
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
