using Serilog;
using System.Diagnostics;

namespace Proxy
{
    /// <summary>Watches the VS process that spawned this proxy and shuts the proxy down once that parent exits, so no orphaned proxy process is left running.</summary>
    public static class ParentProcessWatcher
    {
        private static readonly ILogger _log = SerilogLogger.Logger.ForContext(typeof(ParentProcessWatcher));

        /// <summary>Starts a background wait on <paramref name="parentProcessId"/> and terminates this process once it exits.</summary>
        public static void StartAsync(int parentProcessId)
        {
            _log.Information($"Parent process id: {parentProcessId}");

            _ = Task.Run(
                async () =>
                {
                    try
                    {
                        var parentProcess = Process.GetProcessById(parentProcessId);
                        await parentProcess.WaitForExitAsync();
                    }
                    catch (Exception excp)
                    {
                        //todo log
                        _log.Error(excp, $"!!! ERROR !!!");
                    }

                    Environment.Exit(0);
                });
        }
    }
}
