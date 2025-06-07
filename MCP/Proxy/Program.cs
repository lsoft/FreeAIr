using Proxy.Server;
using Dto;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Serilog;
using System.Reflection;

namespace Proxy
{
    internal class Program
    {
        private static readonly ILogger _log = SerilogLogger.Logger.ForContext<Program>();

        public static Servers Servers = new();

        static async Task Main(string[] args)
        {
            var port = int.Parse(args[0]);
            var visualStudioProcessId = int.Parse(args[1]);

            var workingFolder = new System.IO.FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;

            SerilogLogger.Init(
                workingFolder,
                "Log",
                $"proxy_vspid={visualStudioProcessId}.log"
                );

            _log.Information("Starting...");

            ParentProcessWatcher.StartAsync();

            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.UseUrls($"http://localhost:{port}");

            var app = builder.Build();

            app.MapPost(
                "/update_external_servers",
                async (context) =>
                {
                    _ = await UpdateExternalServersAsync(context);
                }
            );
            app.MapPost(
                "/is_installed",
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
            app.MapPost(
                "/get_tools",
                async (context) =>
                {
                    _ = await GetToolsAsync(context);
                }
            );
            app.MapPost(
                "/call_tool",
                async (context) =>
                {
                    _ = await CallToolAsync(context);
                }
            );

            _log.Information("Running... Press Ctrl+C to stop.");

            app.Run();
        }

        private static async Task<bool> UpdateExternalServersAsync(
            HttpContext context
            )
        {
            try
            {
                var req = await context.Request.ReadFromJsonAsync<UpdateExternalServersRequest>();
                if (req is null)
                {
                    _log.Error("Cannot parse request");
                    await context.Response.WriteAsJsonAsync(
                        BaseReply.FromError<UpdateExternalServersReply>("Cannot parse request")
                        );
                    return false;
                }

                Servers.RemoveAllExternalServers();
                var approvedMcpServers = await Servers.UpdateExternalServersAsync(req.McpServers);

                await context.Response.WriteAsJsonAsync(
                    new UpdateExternalServersReply(
                        approvedMcpServers
                        )
                    );
                return true;
            }
            catch (Exception excp)
            {
                _log.Error(excp, "Error during updating external servers");

                await context.Response.WriteAsJsonAsync(
                    BaseReply.FromError<UpdateExternalServersReply>("Error during updating external servers: " + excp.Message + Environment.NewLine + excp.StackTrace)
                    );
            }

            return false;
        }

        private static async Task<bool> IsInstalledAsync(
            HttpContext context
            )
        {
            try
            {
                var req = await context.Request.ReadFromJsonAsync<IsInstalledRequest>();
                if (req is null)
                {
                    _log.Error("Cannot parse request");
                    await context.Response.WriteAsJsonAsync(
                        BaseReply.FromError<IsInstalledReply>("Cannot parse request")
                        );
                    return false;
                }

                var serverName = req.MCPServerName;
                var server = Servers.GetServer(serverName);

                var result = await server.IsInstalledAsync(
                    req
                    );

                await context.Response.WriteAsJsonAsync(
                    result
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
                        BaseReply.FromError<InstallReply>("Cannot parse request")
                        );
                    return false;
                }

                var serverName = req.MCPServerName;
                var server = Servers.GetServer(serverName);

                var result = await server.InstallAsync(
                    req
                    );

                await context.Response.WriteAsJsonAsync(
                    result
                    );
                return true;
            }
            catch (Exception excp)
            {
                _log.Error(excp, "Error during installation");

                await context.Response.WriteAsJsonAsync(
                    BaseReply.FromError<InstallReply>("Error during installation: " + excp.Message + Environment.NewLine + excp.StackTrace)
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
                var req = await context.Request.ReadFromJsonAsync<GetToolsRequest>();
                if (req is null)
                {
                    _log.Error("Cannot parse request");
                    await context.Response.WriteAsJsonAsync(
                        BaseReply.FromError<GetToolsReply>("Cannot parse request")
                        );
                    return false;
                }

                var serverName = req.MCPServerName;
                var server = Servers.GetServer(serverName);

                var result = await server.GetToolsAsync(
                    req
                    );

                await context.Response.WriteAsJsonAsync(
                    result
                    );

                return true;
            }
            catch (Exception excp)
            {
                _log.Error(excp, "Error during getting tools");

                await context.Response.WriteAsJsonAsync(
                    BaseReply.FromError<GetToolsReply>("Error during getting tools: " + excp.Message + Environment.NewLine + excp.StackTrace)
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
                        BaseReply.FromError<CallToolReply>("Cannot parse request")
                        );
                    return false;
                }

                var serverName = req.MCPServerName;
                var server = Servers.GetServer(serverName);

                var result = await server.CallToolAsync(
                    req,
                    req.ToolName,
                    req.Arguments?.ToDictionary(d => d.Key, d => (object?)d.Value)
                    );

                await context.Response.WriteAsJsonAsync(
                    result
                    );

                return true;
            }
            catch (Exception excp)
            {
                _log.Error(excp, "Error during calling tools");

                await context.Response.WriteAsJsonAsync(
                    BaseReply.FromError<CallToolReply>("Error during calling tools: " + excp.Message + Environment.NewLine + excp.StackTrace)
                    );
            }

            return false;
        }

    }
}
