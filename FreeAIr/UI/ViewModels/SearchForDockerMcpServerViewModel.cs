using Dto;
using FreeAIr.Helper;
using Microsoft.Build.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Windows.Input;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    public sealed class SearchForDockerMcpServerViewModel : BaseViewModel
    {
        private readonly List<DockerMcpServerInfo> _serverList;
        private string _filter;
        private ICommand _installAndSetupCommand;

        public Action<bool>? CloseWindow
        {
            get;
            set;
        }

        public string Filter
        {
            get => _filter;
            set
            {
                _filter = value;

                Refilter();
            }
        }

        public ObservableCollection2<DockerMcpServerInfo> FilteredServerList
        {
            get;
        }

        public DockerMcpServerInfo SelectedServer
        {
            get;
            set;
        }

        public ICommand InstallAndSetupCommand
        {
            get
            {
                if (_installAndSetupCommand is null)
                {
                    _installAndSetupCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            var server = a as DockerMcpServerInfo;

                            (this.McpServerName, this.McpServer) = await ConstructServerAsync(
                                server.ServerName
                                );

                            CloseWindow(true);
                        },
                        a =>
                        {
                            return true;
                        });
                }

                return _installAndSetupCommand;
            }
        }

        public string? McpServerName
        {
            get;
            private set;
        }

        public McpServer? McpServer
        {
            get;
            private set;
        }


        public SearchForDockerMcpServerViewModel(
            )
        {
            _serverList = [];
            FilteredServerList = new ObservableCollection2<DockerMcpServerInfo>();

            RetrieveMcpListAsync()
                .FileAndForget(nameof(RetrieveMcpListAsync));
        }

        private static async System.Threading.Tasks.Task<(string, McpServer)> ConstructServerAsync(
            string serverName
            )
        {
            try
            {
                var installResult = await ProcessHelper.RunSilentlyAsync(
                    Directory.GetCurrentDirectory(),
                    "docker",
                    $"mcp server enable {serverName}",
                    CancellationToken.None
                    );
                if (installResult.ExitCode != 0)
                {
                    await VS.MessageBox.ShowErrorAsync(
                        string.Join(
                            Environment.NewLine,
                            installResult.StandardError
                            )
                        );
                    return (null, null);
                }

                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("C# App");

                var image = await httpClient.GetFromJsonAsync<Image>(
                    $"https://hub.docker.com/v2/repositories/mcp/{serverName}"
                    );
                if (image is null)
                {
                    await VS.MessageBox.ShowWarningAsync(
                        $"Docker image for {serverName} MCP server installed. Please setup FreeAIr to work with the server."
                        );
                    return (null, null);
                }

                var html = image.full_description;

                var index = html.IndexOf("## Use this MCP Server");
                if (index < 0)
                {
                    await VS.MessageBox.ShowWarningAsync(
                        $"Docker image for {serverName} MCP server installed. Please setup FreeAIr to work with the server."
                        );
                    return (null, null);
                }

                const string jsonStart = "```json";
                const string jsonEnd = "```";

                var si = html.IndexOf(jsonStart, index);
                if (si < 0)
                {
                    await VS.MessageBox.ShowWarningAsync(
                        $"Docker image for {serverName} MCP server installed. Please setup FreeAIr to work with the server."
                        );
                    return (null, null);
                }

                var ei = html.IndexOf(jsonEnd, si + jsonStart.Length);
                if (ei < 0)
                {
                    await VS.MessageBox.ShowWarningAsync(
                        $"Docker image for {serverName} MCP server installed. Please setup FreeAIr to work with the server."
                        );
                    return (null, null);
                }

                var json = html
                    .Substring(si + jsonStart.Length, ei - si - jsonStart.Length - jsonEnd.Length + 2)
                    ;

                var mcpServers = System.Text.Json.JsonSerializer.Deserialize<McpServers>(json);
                if (mcpServers.Servers.Count != 1)
                {
                    await VS.MessageBox.ShowWarningAsync(
                        $"Docker image for {serverName} MCP server installed. Please setup FreeAIr to work with the server."
                        );
                    return (null, null);
                }

                var pair = mcpServers.Servers.First();
                return (pair.Key, pair.Value);
            }
            catch (Exception excp)
            {
                //todo log
            }

            return (null, null);
        }

        private async Task RetrieveMcpListAsync()
        {
            _serverList.Clear();

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("C# App");

            string? nextUrl = @"https://hub.docker.com/v2/repositories/mcp?page_size=100";
            do
            {
                try
                {
                    var portion = await httpClient.GetFromJsonAsync<ImagesFromPublisher>(
                        nextUrl
                        );
                    foreach (var mcp in portion.results)
                    {
                        _serverList.Add(
                            new DockerMcpServerInfo(
                                mcp.name,
                                mcp.description
                                )
                            );
                    }

                    nextUrl = portion.next;
                }
                catch (Exception excp)
                {
                    //todo log
                    nextUrl = null;
                }
            }
            while (!string.IsNullOrEmpty(nextUrl));

            Refilter();
        }

        private void Refilter()
        {
            var lfilter = _filter?.ToLower();

            FilteredServerList.Clear();
            foreach (var server in _serverList)
            {
                if (string.IsNullOrEmpty(lfilter) ||
                    (
                        server.ServerName.ToLower().Contains(lfilter)
                        || server.ServerDescription.ToLower().Contains(lfilter)
                    )
                    )
                {
                    FilteredServerList.Add(server);
                }
            }

            OnPropertyChanged();
        }

        public sealed class DockerMcpServerInfo : BaseViewModel
        {
            private ICommand _openBrowserCommand;

            public string ServerName
            {
                get;
            }

            public string ServerDescription
            {
                get;
            }

            public ICommand OpenBrowserCommand
            {
                get
                {
                    if (_openBrowserCommand is null)
                    {
                        _openBrowserCommand = new AsyncRelayCommand(
                            async a =>
                            {
                                Process.Start(
                                    $"https://hub.docker.com/r/mcp/{ServerName}"
                                    );
                            },
                            a =>
                            {
                                return true;
                            });
                    }

                    return _openBrowserCommand;
                }
            }

            public DockerMcpServerInfo(
                string serverName,
                string serverDescription
                )
            {
                if (serverName is null)
                {
                    throw new ArgumentNullException(nameof(serverName));
                }

                if (serverDescription is null)
                {
                    throw new ArgumentNullException(nameof(serverDescription));
                }

                ServerName = serverName;
                ServerDescription = serverDescription;
            }
        }
    }
    
    #region json DTO

    public class Image
    {
        public string user
        {
            get; set;
        }
        public string name
        {
            get; set;
        }
        public string _namespace
        {
            get; set;
        }
        public object repository_type
        {
            get; set;
        }
        public int status
        {
            get; set;
        }
        public string status_description
        {
            get; set;
        }
        public string description
        {
            get; set;
        }
        public bool is_private
        {
            get; set;
        }
        public bool is_automated
        {
            get; set;
        }
        public int star_count
        {
            get; set;
        }
        public int pull_count
        {
            get; set;
        }
        public DateTime last_updated
        {
            get; set;
        }
        public DateTime last_modified
        {
            get; set;
        }
        public DateTime date_registered
        {
            get; set;
        }
        public int collaborator_count
        {
            get; set;
        }
        public object affiliation
        {
            get; set;
        }
        public string hub_user
        {
            get; set;
        }
        public bool has_starred
        {
            get; set;
        }
        public string full_description
        {
            get; set;
        }
        public Permissions permissions
        {
            get; set;
        }
        public string[] media_types
        {
            get; set;
        }
        public string[] content_types
        {
            get; set;
        }
        public Category[] categories
        {
            get; set;
        }
        public Immutable_Tags_Settings immutable_tags_settings
        {
            get; set;
        }
        public long? storage_size
        {
            get; set;
        }
    }

    public class Permissions
    {
        public bool read
        {
            get; set;
        }
        public bool write
        {
            get; set;
        }
        public bool admin
        {
            get; set;
        }
    }

    public class Immutable_Tags_Settings
    {
        public bool enabled
        {
            get; set;
        }
        public string[] rules
        {
            get; set;
        }
    }

    public class ImagesFromPublisher
    {
        public int count
        {
            get; set;
        }
        public string next
        {
            get; set;
        }
        public object previous
        {
            get; set;
        }
        public Image[] results
        {
            get; set;
        }
    }

    public class Category
    {
        public string name
        {
            get; set;
        }
        public string slug
        {
            get; set;
        }
    }

    #endregion
}
