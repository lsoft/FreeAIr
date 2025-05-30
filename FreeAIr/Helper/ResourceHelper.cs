namespace FreeAIr.Helper
{
    public static class ResourceHelper
    {
        public static string GetLocalizedResourceByName(
            this string resourceName
            )
        {
            return FreeAIr.Resources.Resources.ResourceManager.GetString(
                resourceName,
                ResponsePage.GetAnswerCulture()
                );
        }
    }
}
