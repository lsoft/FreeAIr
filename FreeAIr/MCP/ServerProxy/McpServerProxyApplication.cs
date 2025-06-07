using Dto;
using EnvDTE;
using EnvDTE80;
using FreeAIr.McpServerProxy;
using FreeAIr.MCP.McpServerProxy.External;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy
{
    public static class McpServerProxyApplication
    {
        public const string ProxyApplicationZipFileName = "Proxy.zip";
        public const string ProxyApplicationExeFileName = "Proxy.exe";

        private static readonly string _proxyUnpackedFolderPath;
        private static readonly string _proxyZipFolderPath;

        private static readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private static readonly ProcessMonitor _processMonitor;
        private static readonly Task _processTask;

        private static readonly DTEEvents _dteEvents;

        private static readonly HttpClient _httpClient;

        public static readonly bool Started;

        public static HttpClient HttpClient =>
            Started
                ? _httpClient
                : throw new InvalidOperationException("Proxy not started");

        static McpServerProxyApplication()
        {
            _proxyUnpackedFolderPath = Path.Combine(FreeAIrPackage.WorkingFolder, @"MCP\Proxy\Unpacked");
            _proxyZipFolderPath = Path.Combine(FreeAIrPackage.WorkingFolder, @"MCP\Proxy\Archive");

            UnpackProxy();

            var visualStudioProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
            var proxyProcessId = 30000 + (visualStudioProcessId % 10000);

            _httpClient = new();
            _httpClient.BaseAddress = new Uri($"http://localhost:{proxyProcessId}");

            _processMonitor = new ProcessMonitor(
                _proxyUnpackedFolderPath,
                ProxyApplicationExeFileName,
                $"{proxyProcessId} {visualStudioProcessId}"
                );

            _processTask = _processMonitor.StartMonitoringAsync(
                _cancellationTokenSource.Token
                );

            var dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
            _dteEvents = ((Events2)dte.Events).DTEEvents;
            _dteEvents.OnBeginShutdown += DTEEvents_OnBeginShutdown;

            Started = true;
        }

        public static async Task<McpServersSetupConfigurationResult?> UpdateExternalServersAsync(
            McpServers? mcpServers = null
            )
        {
            if (mcpServers is null)
            {
                if (!ExternalMcpServersJsonParser.TryParse(
                    MCPPage.Instance.ExternalMCPServers,
                    out mcpServers))
                {
                    return null;
                }
            }

            var response = await McpServerProxyApplication.HttpClient.PostAsJsonAsync<UpdateExternalServersRequest>(
                "/update_external_servers",
                new UpdateExternalServersRequest(
                    mcpServers
                    )
                );
            response.EnsureSuccessStatusCode();

            var reply = await response.Content.ReadFromJsonAsync<UpdateExternalServersReply>();
            if (!string.IsNullOrEmpty(reply.ErrorMessage))
            {
                throw new InvalidOperationException(reply.ErrorMessage);
            }

            var approvedExternalMcpServers = reply.McpServers;

            var toolContainer = AvailableToolContainer.ReadSystem();

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

        private static void UnpackProxy()
        {
            if (!Directory.Exists(_proxyUnpackedFolderPath))
            {
                Directory.CreateDirectory(_proxyUnpackedFolderPath);

                var zipFilePath = Path.Combine(
                    FreeAIrPackage.WorkingFolder,
                    _proxyZipFolderPath,
                    ProxyApplicationZipFileName
                    );
                using var zip = ZipFile.OpenRead(zipFilePath);
                zip.ExtractToDirectory(_proxyUnpackedFolderPath);
            }
        }

    }
}
