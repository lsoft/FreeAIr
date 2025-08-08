namespace FreeAIr.Helper
{
    public static class UriHelper
    {
        public static Uri? TryBuildEndpointUri(string endpoint)
        {
            try
            {
                return new Uri(endpoint);
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }

            return null;
        }

    }
}
