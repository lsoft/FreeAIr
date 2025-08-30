namespace FreeAIr.Helper
{
    public static class DisposeHelper
    {
        public static void SafelyDispose(
            this IDisposable d
            )
        {
            try
            {
                d.Dispose();
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }
        }
    }
}
