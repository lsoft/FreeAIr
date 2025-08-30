using Proxy.Server;
using Serilog;
using StreamJsonRpc;
using System.Diagnostics;
using System.Reflection;

namespace Proxy
{
    internal class Program
    {
        private static readonly ILogger _log = SerilogLogger.Logger.ForContext<Program>();

        public static Servers Servers = new();

        static async Task<int> Main(string[] args)
        {
            var parentProcessId = GetParentProcessId();
            if (!parentProcessId.HasValue)
            {
                return -1;
            }

            var workingFolder = new System.IO.FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;

            SerilogLogger.Init(
                workingFolder,
                "Log",
                $"proxy_vspid={parentProcessId}.log"
                );

            _log.Information($"Starting with Id = {Process.GetCurrentProcess().Id}...");

            ParentProcessWatcher.StartAsync(parentProcessId.Value);

            _log.Information("Running... Press Ctrl+C to stop.");

            var rpc = JsonRpc.Attach(
                Console.OpenStandardOutput(),
                Console.OpenStandardInput(),
                new McpProxyInterface(
                    Servers
                    )
                );

            while (true)
            {
                Thread.Sleep(1_000);
            }
        }

        /// <summary>
        /// Получение ID родительского процесса через WMI
        /// </summary>
        private static int? GetParentProcessId()
        {
            using (var process = Process.GetCurrentProcess())
            {
                try
                {
                    string query = $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {process.Id}";
                    using (var searcher = new System.Management.ManagementObjectSearcher(query))
                    {
                        foreach (var item in searcher.Get())
                        {
                            return Convert.ToInt32(item["ParentProcessId"]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    //todo log
                    _log.Error(ex, $"Ошибка при получении родительского PID");
                }
            }

            return null;
        }
    }
}
