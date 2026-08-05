namespace FreeAIr.Helper
{
    /// <summary>
    /// Provides a `Dispose` that never throws, for cleanup paths (e.g. `finally` blocks and shutdown
    /// handlers) where a disposal failure must not mask or replace the original error.
    /// </summary>
    public static class DisposeHelper
    {
        /// <summary>
        /// Disposes the object, logging rather than throwing if <see cref="IDisposable.Dispose"/>
        /// itself fails.
        /// </summary>
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
