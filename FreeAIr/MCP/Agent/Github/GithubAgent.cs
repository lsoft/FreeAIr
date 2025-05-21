using Dto;
using FreeAIr.Agent;
using FreeAIr.MCP.Agent.Github.BLO;
using FreeAIr.UI.ViewModels;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.Agent.Github
{
    public sealed class GithubAgent
    {
        public const string MCPServerFolderName = @"MCP\Github\Server";

        private readonly ProcessMonitor _processMonitor;

        private readonly HttpClient _client;
        private Task? _processTask;

        public static readonly string AgentUnpackedFolderPath = Path.Combine(FreeAIrPackage.WorkingFolder, @"MCP\Github\Agent\Unpacked");

        public static readonly string AgentZipFolderPath = Path.Combine(FreeAIrPackage.WorkingFolder, @"MCP\Github\Agent\Archive");
        public const string AgentZipFileName = "Github.Agent.zip";
        public const string AgentExeFileName = "Agent.exe";

        public static readonly GithubAgent Instance = new();

        private GithubAgent(
            )
        {
            UnpackAgent();

            var agentProcessId = 30000 + (System.Diagnostics.Process.GetCurrentProcess().Id % 10000);

            _client = new();
            _client.BaseAddress = new Uri($"https://localhost:{agentProcessId}");

            _processMonitor = new ProcessMonitor(
                AgentUnpackedFolderPath,
                AgentExeFileName,
                agentProcessId.ToString()
                );

            _processTask = _processMonitor.StartMonitoringAsync();
        }

        public async Task<bool> IsInstalledAsync()
        {
            if (_processTask is null)
            {
                throw new InvalidOperationException("Github MCP server not started.");
            }

            var queryParams = new Dictionary<string, string>()
            {
                { "mcpServerFolderPath", MCPServerFolderPath }
            };

            var requestUrl = QueryHelpers.AddQueryString(
                "/install",
                queryParams
                );

            var reply = await _client.GetFromJsonAsync<IsInstalledReply>(
                requestUrl
                );
            return reply.IsInstalled;
        }

        public async Task InstallAsync()
        {
            if (_processTask is null)
            {
                throw new InvalidOperationException("Github MCP server not started.");
            }

            var response = await _client.PostAsJsonAsync<InstallRequest>(
                "/install",
                new InstallRequest(MCPServerFolderPath)
                );
            response.EnsureSuccessStatusCode();

            var reply = await response.Content.ReadFromJsonAsync<InstallReply>();
            if (!string.IsNullOrEmpty(reply.ErrorMessage))
            {
                throw new InvalidOperationException(reply.ErrorMessage);
            }
        }

        public async Task<AgentTools> GetToolsAsync()
        {
            if (_processTask is null)
            {
                throw new InvalidOperationException("Github MCP server not started.");
            }

            var queryParams = new Dictionary<string, string>()
            {
                { "mcpServerFolderPath", MCPServerFolderPath },
                { "githubToken", MCPPage.Instance.GitHubToken}
            };

            var requestUrl = QueryHelpers.AddQueryString(
                "/tools",
                queryParams
                );

            var reply = await _client.GetFromJsonAsync<GetToolsReply>(
                requestUrl
                );
            if (!string.IsNullOrEmpty(reply.ErrorMessage))
            {
                throw new InvalidOperationException(reply.ErrorMessage);
            }

            return new AgentTools(
                reply.AgentName,
                reply.Tools.Select(t => new AgentTool(t.Name, t.Description, t.Parameters)).ToList()
                );
        }

        public async Task<AgentToolCallResult> CallToolAsync(
            string toolName,
            IReadOnlyDictionary<string, object?>? arguments = null
            )
        {
            if (_processTask is null)
            {
                throw new InvalidOperationException("Github MCP server not started.");
            }

            var response = await _client.PostAsJsonAsync<CallToolRequest>(
                "/tool",
                new CallToolRequest(
                    MCPServerFolderPath,
                    MCPPage.Instance.GitHubToken,
                    toolName,
                    arguments?.ToDictionary(d => d.Key, d => d.Value as string)
                    )
                );
            response.EnsureSuccessStatusCode();

            var reply = await response.Content.ReadFromJsonAsync<CallToolReply>();
            if (!string.IsNullOrEmpty(reply.ErrorMessage))
            {
                throw new InvalidOperationException(reply.ErrorMessage);
            }
            if (reply.IsError)
            {
                throw new InvalidOperationException("Error during tool call");
            }

            return new AgentToolCallResult(
                reply.Content
                );
        }


        public async Task WaitForStopAsync()
        {
            if (_processTask is null)
            {
                return;
            }
            if (_processTask.IsCompleted)
            {
                return;
            }

            await _processTask;
        }

        public static string MCPServerFolderPath
        {
            get
            {
                return Path.Combine(
                    FreeAIrPackage.WorkingFolder,
                    MCPServerFolderName
                    );
            }
        }

        private void UnpackAgent()
        {
            try
            {
                if (!Directory.Exists(AgentUnpackedFolderPath))
                {
                    Directory.CreateDirectory(AgentUnpackedFolderPath);

                    var zipFilePath = Path.Combine(
                        FreeAIrPackage.WorkingFolder,
                        AgentZipFolderPath,
                        AgentZipFileName
                        );
                    using var zip = ZipFile.OpenRead(zipFilePath);
                    zip.ExtractToDirectory(AgentUnpackedFolderPath);
                }
            }
            catch (Exception excp)
            {
                //todo log
                int g = 0;
            }
        }

    }
}
