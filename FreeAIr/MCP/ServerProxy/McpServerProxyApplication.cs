using Dto;
using EnvDTE;
using EnvDTE80;
using FreeAIr.Helper;
using FreeAIr.McpServerProxy;
using FreeAIr.Options2;
using StreamJsonRpc;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy
{
    /// <summary>
    /// Owns `Proxy.exe` — the child process which hosts every out-of-process MCP server.
    ///
    /// MCP servers cannot be hosted inside devenv: the extension targets .NET Framework 4.8, while
    /// the MCP SDK needs a modern .NET. So the proxy is a .NET 9 executable shipped inside the VSIX
    /// as `Proxy.zip`, unpacked on the first use and talked to over JSON-RPC on its stdin/stdout.
    ///
    /// Everything here happens in the static constructor, which means that merely touching this
    /// class starts the child process.
    /// </summary>
    public static class McpServerProxyApplication
    {
        public const string ProxyApplicationZipFileName = "Proxy.zip";
        public const string ProxyApplicationExeFileName = "Proxy.exe";

        public static readonly string ProxyUnpackedFolderPath;
        private static readonly string _proxyZipFolderPath;

        private static readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private static readonly ProcessMonitor _processMonitor;
        private static readonly Task _processTask;

        private static readonly DTEEvents _dteEvents;

        //private static readonly HttpClient _httpClient;

        public static readonly bool Started;
        private static IMcpProxyInterface _proxyInterface;

        public static IMcpProxyInterface ProxyInterface => _proxyInterface;

        //public static HttpClient HttpClient =>
        //    Started
        //        ? _httpClient
        //        : throw new InvalidOperationException("Proxy not started");

        static McpServerProxyApplication()
        {
            ProxyUnpackedFolderPath = Path.Combine(FreeAIrPackage.WorkingFolder, @"MCP\Proxy\Unpacked");
            _proxyZipFolderPath = Path.Combine(FreeAIrPackage.WorkingFolder, @"MCP\Proxy\Archive");

            UnpackProxy();

            //derive a per-devenv identifier so that several Visual Studio instances
            //running side by side do not fight over the same proxy
            var visualStudioProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
            var proxyProcessId = 30000 + (visualStudioProcessId % 10000);

            //_httpClient = new();
            //_httpClient.BaseAddress = new Uri($"http://localhost:{proxyProcessId}");

            _processMonitor = new ProcessMonitor(
                ProxyUnpackedFolderPath,
                ProxyApplicationExeFileName,
                $"{proxyProcessId} {visualStudioProcessId}"
                );

            _processTask = _processMonitor.StartMonitoringAsync(
                _cancellationTokenSource.Token
                );

            var rpc = JsonRpc.Attach(
                _processMonitor.Process.StandardInput.BaseStream,
                _processMonitor.Process.StandardOutput.BaseStream
                );
            _proxyInterface = rpc.Attach<IMcpProxyInterface>();

            var dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
            _dteEvents = ((Events2)dte.Events).DTEEvents;
            _dteEvents.OnBeginShutdown += DTEEvents_OnBeginShutdown;

            Started = true;
        }

        /// <summary>
        /// Pushes the configured MCP servers into the proxy, then reconciles the local tool
        /// catalogue with what actually started.
        ///
        /// Servers the proxy refused to start are simply missing from the result, which is how the
        /// caller learns about the failure — see
        /// <see cref="McpServersSetupConfigurationResult.SuccessStartedMcpServers"/>.
        /// </summary>
        /// <param name="mcpServers">
        /// The servers to run. When null they are taken from the current FreeAIr settings.
        /// </param>
        /// <returns>Null when the settings could not even be read.</returns>
        public static async Task<McpServersSetupConfigurationResult?> UpdateExternalServersAsync(
            McpServers? mcpServers = null
            )
        {
            if (mcpServers is null)
            {
                try
                {
                    mcpServers = await FreeAIrOptions.DeserializeMcpServersAsync();
                }
                catch (Exception excp)
                {
                    excp.ActivityLogException();

                    return null;
                }
            }

            var reply = await _proxyInterface.UpdateExternalServersAsync(
                new UpdateExternalServersRequest(
                    mcpServers
                    )
                );
            if (!string.IsNullOrEmpty(reply.ErrorMessage))
            {
                ActivityLogHelper.ActivityLogWarning(reply.ErrorMessage);
            }

            var approvedExternalMcpServers = reply.McpServers ?? new McpServers();

            var toolContainer = await AvailableToolContainer.ReadSystemAsync();

            var setupResult = await McpServerProxyCollection.SetupConfigurationAsync(
                toolContainer,
                approvedExternalMcpServers
                );

            return setupResult;
        }

        private static void DTEEvents_OnBeginShutdown()
        {
            _cancellationTokenSource.Cancel();

            ////а зачем вызывать WaitForStopAsync и НЕ ждать его?
            //WaitForStopAsync()
            //    .FileAndForget(nameof(WaitForStopAsync));
        }

        //public async Task WaitForStopAsync()
        //{
        //    if (_processTask is null)
        //    {
        //        return;
        //    }
        //    if (_processTask.IsCompleted || _processTask.IsCanceled || _processTask.IsFaulted)
        //    {
        //        return;
        //    }

        //    await _processTask;
        //}

        /// <summary>
        /// Extracts `Proxy.zip` into the extension folder. The presence of the target folder is
        /// taken as "already unpacked", so an upgraded VSIX unpacks into a new folder rather than
        /// merging into the old one.
        /// </summary>
        private static void UnpackProxy()
        {
            if (!Directory.Exists(ProxyUnpackedFolderPath))
            {
                Directory.CreateDirectory(ProxyUnpackedFolderPath);

                var zipFilePath = Path.Combine(
                    FreeAIrPackage.WorkingFolder,
                    _proxyZipFolderPath,
                    ProxyApplicationZipFileName
                    );
                using var zip = ZipFile.OpenRead(zipFilePath);
                zip.ExtractToDirectory(ProxyUnpackedFolderPath);
            }
        }

    }
}
