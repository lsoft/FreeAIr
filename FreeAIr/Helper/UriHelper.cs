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
                //todo log
            }

            return null;
        }

    }
}
