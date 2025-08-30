using Serilog;
using System.Diagnostics;

namespace Proxy
{
    public static class ParentProcessWatcher
    {
        private static readonly ILogger _log = SerilogLogger.Logger.ForContext(typeof(ParentProcessWatcher));

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
