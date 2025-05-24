using Agent.BLogic;
using Dto;
using Json.Path;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Serilog;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Agent
{
    internal class Program
    {
        private static readonly ILogger _log = SerilogLogger.Logger.ForContext<Program>();

        static async Task Main(string[] args)
        {

            //var mcpClient = await McpClient.CreateClientAsync(
            //    @"C:\Temp\_github_mcp",
            //    "token here"
            //    );
            //if (mcpClient is null)
            //{
            //    return;
            //}

            //var tool = (await mcpClient.ListToolsAsync()).First(t => t.Name == "get_issue");
            //var json = tool.JsonSchema;

            //var jsonPath = JsonPath.Parse("$.properties.*");

            //var instance = JsonNode.Parse(
            //    json.GetRawText()
            //    );
            //var evaluateds = jsonPath.Evaluate(instance);
            //foreach (var match in evaluateds.Matches)
            //{
            //    var fp = new FunctionParameter(match.Value);

            //}


            //var converted = new Dictionary<string, object?>();
            //foreach (var parameter in tool.AdditionalProperties)
            //{
            //    int g = 0;
            //}

            //var result = await mcpClient.CallToolAsync(
            //    "get_issue",
            //    new Dictionary<string, object?>
            //    {
            //        ["owner"] = "lsoft",
            //        ["repo"] = "FreeAIr",
            //        ["issue_number"] = 1
            //    }
            //    );

            var port = int.Parse(args[0]);
            var visualStudioProcessId = int.Parse(args[1]);

            var workingFolder = new System.IO.FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;

            SerilogLogger.Init(
                workingFolder,
                "Log",
                $"github_agent_vspid={visualStudioProcessId}.log"
                );

            _log.Information("Starting...");


            ParentProcessWatcher.StartAsync();

            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.UseUrls($"https://localhost:{port}");

            var app = builder.Build();

            app.MapGet(
                "/install",
                async (context) =>
                {
                    _ = await IsInstalledAsync(context);
                }
            );
            app.MapPost(
                "/install",
                async (context) =>
                {
                    _ = await InstallAsync(context);
                }
            );
            app.MapGet(
                "/tools",
                async (context) =>
                {
                    _ = await GetToolsAsync(context);
                }
            );
            app.MapPost(
                "/tool",
                async (context) =>
                {
                    _ = await CallToolAsync(context);
                }
            );

            _log.Information("Running... Press Ctrl+C to stop.");

            app.Run();
        }

        private static async Task<bool> IsInstalledAsync(
            HttpContext context
            )
        {
            try
            {
                var mcpServerFolderPath = context.Request.Query["mcpServerFolderPath"];

                var result = Installer.IsInstalled(
                    mcpServerFolderPath
                    );

                await context.Response.WriteAsJsonAsync(
                    new IsInstalledReply(result)
                    );
                return true;
            }
            catch (Exception excp)
            {
                _log.Error(excp, "Error during installation");

                await context.Response.WriteAsJsonAsync(
                    new IsInstalledReply(false)
                    );
            }

            return false;
        }

        private static async Task<bool> InstallAsync(
            HttpContext context
            )
        {
            try
            {
                var req = await context.Request.ReadFromJsonAsync<InstallRequest>();
                if (req is null)
                {
                    _log.Error("Cannot parse request");
                    await context.Response.WriteAsJsonAsync(
                        Reply.FromError<InstallReply>("Cannot parse request")
                        );
                    return false;
                }

                var result = await Installer.InstallAsync(
                    req.MCPServerFolderPath
                    );
                if (!result)
                {
                    _log.Error("Cannot during installation");
                    await context.Response.WriteAsJsonAsync(
                        Reply.FromError<InstallReply>("Error during installation")
                        );
                    return false;
                }

                await context.Response.WriteAsJsonAsync(
                    new InstallReply()
                    );
                return true;
            }
            catch (Exception excp)
            {
                _log.Error(excp, "Error during installation");

                await context.Response.WriteAsJsonAsync(
                    Reply.FromError<InstallReply>("Error during installation: " + excp.Message + Environment.NewLine + excp.StackTrace)
                    );
            }

            return false;
        }

        private static async Task<bool> GetToolsAsync(
            HttpContext context
            )
        {
            try
            {
                var mcpServerFolderPath = context.Request.Query["mcpServerFolderPath"];
                var githubToken = context.Request.Query["githubToken"];

                var mcpClient = await McpClient.CreateClientAsync(
                    mcpServerFolderPath,
                    githubToken
                    );
                if (mcpClient is null)
                {
                    _log.Error("Cannot create McpClient");
                    await context.Response.WriteAsJsonAsync(
                        Reply.FromError<GetToolsReply>("Cannot create McpClient")
                        );
                    return false;
                }

                var mcpTools = await mcpClient.ListToolsAsync();

                var reply = new GetToolsReply(
                    mcpTools.Select(t => new GetToolReply(t.Name, t.Description, t.JsonSchema.GetRawText())).ToArray()
                    );

                await context.Response.WriteAsJsonAsync(
                    reply
                    );

                return true;
            }
            catch (Exception excp)
            {
                _log.Error(excp, "Error during getting tools");

                await context.Response.WriteAsJsonAsync(
                    Reply.FromError<GetToolsReply>("Error during getting tools: " + excp.Message + Environment.NewLine + excp.StackTrace)
                    );
            }

            return false;
        }

        private static async Task<bool> CallToolAsync(
            HttpContext context
            )
        {
            try
            {
                var req = await context.Request.ReadFromJsonAsync<CallToolRequest>();
                if (req is null)
                {
                    _log.Error("Cannot parse request");
                    await context.Response.WriteAsJsonAsync(
                        Reply.FromError<CallToolReply>("Cannot parse request")
                        );
                    return false;
                }

                var mcpClient = await McpClient.CreateClientAsync(
                    req.MCPServerFolderPath,
                    req.GithubToken
                    );
                if (mcpClient is null)
                {
                    _log.Error("Cannot create McpClient");
                    await context.Response.WriteAsJsonAsync(
                        Reply.FromError<CallToolReply>("Cannot create McpClient")
                        );
                    return false;
                }

                var result = await mcpClient.CallToolAsync(
                    req.ToolName,
                    req.Arguments?.ToDictionary(d => d.Key, d => (object?)d.Value)
                    );

                var texts = result.Content
                    .Where(t => t.Type == "text")
                    .Select(t => t.Text)
                    .ToArray()
                    ;

                if (result.IsError)
                {
                    var msg = "Error call tool: " + string.Join(string.Empty, texts);
                    _log.Error(msg);
                    await context.Response.WriteAsJsonAsync(
                        Reply.FromError<CallToolReply>(msg)
                        );
                    return false;
                }

                await context.Response.WriteAsJsonAsync(
                    new CallToolReply(
                        false,
                        texts
                        )
                    );

                return true;
            }
            catch (Exception excp)
            {
                _log.Error(excp, "Error during calling tools");

                await context.Response.WriteAsJsonAsync(
                    Reply.FromError<CallToolReply>("Error during calling tools: " + excp.Message + Environment.NewLine + excp.StackTrace)
                    );
            }

            return false;
        }

    }
}
