﻿using Dto;
using EnvDTE;
using EnvDTE80;
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
    public sealed class GithubAgent : IAgent
    {
        public const string MCPServerFolderName = @"MCP\Github\Server";

        public const string AgentZipFileName = "Github.Agent.zip";
        public const string AgentExeFileName = "Agent.exe";

        public static readonly GithubAgent Instance = new();

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly string _agentUnpackedFolderPath;
        private readonly string _agentZipFolderPath;
        private readonly HttpClient _client;
        private readonly ProcessMonitor _processMonitor;
        private readonly Task _processTask;
        private readonly bool _started;

        private readonly DTEEvents _dteEvents;

        public string Name => "github.com";

        private GithubAgent(
            )
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                _agentUnpackedFolderPath = Path.Combine(FreeAIrPackage.WorkingFolder, @"MCP\Github\Agent\Unpacked");
                _agentZipFolderPath = Path.Combine(FreeAIrPackage.WorkingFolder, @"MCP\Github\Agent\Archive");

                UnpackAgent();

                var visualStudioProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
                var agentProcessId = 30000 + (visualStudioProcessId % 10000);

                _client = new();
                _client.BaseAddress = new Uri($"https://localhost:{agentProcessId}");

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

                _started = true;
            }
            catch (Exception excp)
            {
                //todo log
                _started = false;
            }
        }

        public async Task<bool> IsInstalledAsync()
        {
            if (!_started)
            {
                return false;
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
            if (!_started)
            {
                throw new InvalidOperationException("Agent is not started");
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
            if (!_started)
            {
                throw new InvalidOperationException("Agent is not started");
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
                reply.Tools.Select(t => new AgentTool(Name, t.Name, t.Description, t.Parameters)).ToList()
                );
        }

        public async Task<AgentToolCallResult> CallToolAsync(
            string toolName,
            Dictionary<string, object?>? arguments = null,
            CancellationToken cancellationToken = default
            )
        {
            if (!_started)
            {
                throw new InvalidOperationException("Agent is not started");
            }

            try
            {
                var response = await _client.PostAsJsonAsync<CallToolRequest>(
                    "/tool",
                    new CallToolRequest(
                        MCPServerFolderPath,
                        MCPPage.Instance.GitHubToken,
                        toolName,
                        arguments
                        ),
                    cancellationToken
                    );
                response.EnsureSuccessStatusCode();

                var reply = await response.Content.ReadFromJsonAsync<CallToolReply>(
                    cancellationToken
                    );
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
            catch (Exception excp)
            {
                //todo log
                return new AgentToolCallResult([excp.Message + Environment.NewLine + excp.StackTrace]);
            }

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

        private void DTEEvents_OnBeginShutdown()
        {
            _cancellationTokenSource.Cancel();

            ////а зачем вызывать WaitForStopAsync и НЕ ждать его?
            //WaitForStopAsync()
            //    .FileAndForget(nameof(WaitForStopAsync));
        }


        private void UnpackAgent()
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
