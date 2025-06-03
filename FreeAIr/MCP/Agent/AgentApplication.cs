using Dto;
using EnvDTE;
using EnvDTE80;
using FreeAIr.Agent;
using FreeAIr.MCP.Agent.External;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.Agent
{
    public static class AgentApplication
    {
        public const string AgentZipFileName = "Agent.zip";
        public const string AgentExeFileName = "Agent.exe";

        private static readonly string _agentUnpackedFolderPath;
        private static readonly string _agentZipFolderPath;

        private static readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private static readonly ProcessMonitor _processMonitor;
        private static readonly Task _processTask;

        private static readonly DTEEvents _dteEvents;

        private static readonly HttpClient _httpClient;

        public static readonly bool Started;

        public static HttpClient HttpClient =>
            Started
                ? _httpClient
                : throw new InvalidOperationException("Agent not started");

        static AgentApplication()
        {
            _agentUnpackedFolderPath = Path.Combine(FreeAIrPackage.WorkingFolder, @"MCP\Agent\Unpacked");
            _agentZipFolderPath = Path.Combine(FreeAIrPackage.WorkingFolder, @"MCP\Agent\Archive");

            UnpackAgent();

            var visualStudioProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
            var agentProcessId = 30000 + (visualStudioProcessId % 10000);

            _httpClient = new();
            _httpClient.BaseAddress = new Uri($"http://localhost:{agentProcessId}");

            _processMonitor = new ProcessMonitor(
                _agentUnpackedFolderPath,
                AgentExeFileName,
                $"{agentProcessId} {visualStudioProcessId}"
                );

            _processTask = _processMonitor.StartMonitoringAsync(
                _cancellationTokenSource.Token
                );

            var dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
            _dteEvents = ((Events2)dte.Events).DTEEvents;
            _dteEvents.OnBeginShutdown += DTEEvents_OnBeginShutdown;

            Started = true;
        }

        public static async Task<AgentSetupConfigurationResult?> UpdateExternalServersAsync(
            McpServers? mcpServers = null
            )
        {
            if (mcpServers is null)
            {
                if (!ExternalAgentJsonParser.TryParse(
                    MCPPage.Instance.ExternalMCPServers,
                    out mcpServers))
                {
                    return null;
                }
            }

            var response = await AgentApplication.HttpClient.PostAsJsonAsync<UpdateExternalServersRequest>(
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

            var setupResult = await AgentCollection.SetupConfigurationAsync(
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

        private static void UnpackAgent()
        {
            if (!Directory.Exists(_agentUnpackedFolderPath))
            {
                Directory.CreateDirectory(_agentUnpackedFolderPath);

                var zipFilePath = Path.Combine(
                    FreeAIrPackage.WorkingFolder,
                    _agentZipFolderPath,
                    AgentZipFileName
                    );
                using var zip = ZipFile.OpenRead(zipFilePath);
                zip.ExtractToDirectory(_agentUnpackedFolderPath);
            }
        }

    }
}
