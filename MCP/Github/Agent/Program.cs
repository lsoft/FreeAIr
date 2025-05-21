using Dto;
using Agent.BLogic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Agent
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var port = int.Parse(args[0]);

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

            Console.WriteLine("Running... Press Ctrl+C to stop.");

            app.Run();
        }

        private static async Task<bool> IsInstalledAsync(
            HttpContext context
            )
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(
                "/is installed"
                );
            Console.ResetColor();
            Console.WriteLine(
                );

            var mcpServerFolderPath = context.Request.Query["mcpServerFolderPath"];

            var result = Installer.IsInstalled(
                mcpServerFolderPath
                );

            await context.Response.WriteAsJsonAsync(
                new IsInstalledReply(result)
                );
            return true;
        }

        private static async Task<bool> InstallAsync(
            HttpContext context
            )
        {
            var req = await context.Request.ReadFromJsonAsync<InstallRequest>();
            if (req is null)
            {
                await context.Response.WriteAsJsonAsync(
                    Reply.FromError<InstallReply>("Cannot parse request")
                    );
                return false;
            }

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(
                System.Text.Json.JsonSerializer.Serialize(req)
                );
            Console.ResetColor();
            Console.WriteLine(
                );

            var result = await Installer.InstallAsync(
                req.MCPServerFolderPath
                );
            if (!result)
            {
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

        private static async Task<bool> GetToolsAsync(
            HttpContext context
            )
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(
                "/tools get"
                );
            Console.ResetColor();
            Console.WriteLine(
                );

            var mcpServerFolderPath = context.Request.Query["mcpServerFolderPath"];
            var githubToken = context.Request.Query["githubToken"];

            var mcpClient = await McpClient.CreateClientAsync(
                mcpServerFolderPath,
                githubToken
                );
            if (mcpClient is null)
            {
                await context.Response.WriteAsJsonAsync(
                    Reply.FromError<GetToolsReply>("Cannot create McpClient")
                    );
                return false;
            }

            var mcpTools = await mcpClient.ListToolsAsync();

            var reply = new GetToolsReply(
                mcpClient.ServerInfo.Name,
                mcpTools.Select(t => new GetToolReply(t.Name, t.Description, t.JsonSchema.GetRawText())).ToArray()
                );

            await context.Response.WriteAsJsonAsync(
                reply
                );

            return true;
        }

        private static async Task<bool> CallToolAsync(
            HttpContext context
            )
        {
            var req = await context.Request.ReadFromJsonAsync<CallToolRequest>();
            if (req is null)
            {
                await context.Response.WriteAsJsonAsync(
                    Reply.FromError<CallToolReply>("Cannot parse request")
                    );
                return false;
            }

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(
                System.Text.Json.JsonSerializer.Serialize(req)
                );
            Console.ResetColor();
            Console.WriteLine(
                );

            var mcpClient = await McpClient.CreateClientAsync(
                req.MCPServerFolderPath,
                req.GithubToken
                );
            if (mcpClient is null)
            {
                await context.Response.WriteAsJsonAsync(
                    Reply.FromError<CallToolReply>("Cannot create McpClient")
                    );
                return false;
            }

            var result = await mcpClient.CallToolAsync(
                req.ToolName,
                req.Arguments?.ToDictionary(d => d.Key, d => (object?)d.Value)
                );

            await context.Response.WriteAsJsonAsync(
                new CallToolReply(
                    result.IsError,
                    result.Content.Where(t => t.Type == "text").Select(t => t.Text).ToArray()
                    )
                );

            return true;
        }

    }
}
