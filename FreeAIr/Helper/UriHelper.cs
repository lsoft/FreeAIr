namespace FreeAIr.Helper
{
    /// <summary>
    /// Helper for safely parsing a user-configured model/MCP endpoint address into a <see cref="Uri"/>.
    /// </summary>
    public static class UriHelper
    {
        /// <summary>
        /// Parses <paramref name="endpoint"/> as a <see cref="Uri"/>, logging to the activity log
        /// and returning <c>null</c> instead of throwing when the endpoint string is malformed.
        /// </summary>
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
