using Dto;
using FreeAIr.Helper;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Windows.Input;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    /// <summary>
    /// View model for the "search Docker MCP servers" dialog: lists MCP server images published
    /// on Docker Hub under the `mcp` namespace, lets the user filter them by name/description,
    /// and installs the selected one via `docker mcp server enable`, extracting its
    /// <see cref="McpServer"/> configuration from the image's description page.
    /// </summary>
    public sealed class SearchForDockerMcpServerViewModel : BaseViewModel
    {
        /// <summary>
        /// The full, unfiltered list of Docker MCP server entries fetched from Docker Hub.
        /// </summary>
        private readonly List<DockerMcpServerInfo> _serverList;

        /// <summary>
        /// Callback invoked to close the dialog, passed whether it closed successfully (a server
        /// was installed) or was cancelled.
        /// </summary>
        public Action<bool>? CloseWindow
        {
            get;
            set;
        }

        /// <summary>
        /// The text typed into the search box; setting it re-filters <see cref="FilteredServerList"/>
        /// by server name or description.
        /// </summary>
        public string Filter
        {
            get;
            set
            {
                field = value;

                Refilter();
            }
        }

        /// <summary>
        /// The subset of <see cref="_serverList"/> matching the current <see cref="Filter"/>,
        /// bound to the dialog's server list box.
        /// </summary>
        public ObservableCollection2<DockerMcpServerInfo> FilteredServerList
        {
            get;
        }

        /// <summary>
        /// The Docker MCP server entry currently highlighted in the list.
        /// </summary>
        public DockerMcpServerInfo SelectedServer
        {
            get;
            set;
        }

        /// <summary>
        /// Command that installs the given Docker MCP server, populates
        /// <see cref="McpServerName"/>/<see cref="McpServer"/> from it, and closes the dialog
        /// with success.
        /// </summary>
        public ICommand InstallAndSetupCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(
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

                return field;
            }
        }

        /// <summary>
        /// The name under which the installed MCP server should be registered, set once
        /// installation succeeds.
        /// </summary>
        public string? McpServerName
        {
            get;
            private set;
        }

        /// <summary>
        /// The MCP server configuration extracted from the installed Docker image's description,
        /// set once installation succeeds.
        /// </summary>
        public McpServer? McpServer
        {
            get;
            private set;
        }


        /// <summary>
        /// Creates the view model with an empty server list and starts fetching the list of
        /// published Docker MCP server images in the background.
        /// </summary>
        public SearchForDockerMcpServerViewModel(
            )
        {
            _serverList = [];
            FilteredServerList = new ObservableCollection2<DockerMcpServerInfo>();

            RetrieveMcpListAsync()
                .FileAndForget(nameof(RetrieveMcpListAsync));
        }

        /// <summary>
        /// Enables the named MCP server via the `docker mcp server enable` CLI, then fetches its
        /// Docker Hub description and parses the JSON MCP configuration out of the
        /// "Use this MCP Server" section, returning the server's name and <see cref="McpServer"/>
        /// configuration (or nulls, with a warning/error dialog, if any step fails).
        /// </summary>
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
                        string.Format(
                            FreeAIr.Resources.Resources.Docker_image_for__0__MCP_server_installed,
                            serverName
                            )
                        );
                    return (null, null);
                }

                var html = image.full_description;

                var index = html.IndexOf("## Use this MCP Server");
                if (index < 0)
                {
                    await VS.MessageBox.ShowWarningAsync(
                        string.Format(
                            FreeAIr.Resources.Resources.Docker_image_for__0__MCP_server_installed,
                            serverName
                            )
                        );
                    return (null, null);
                }

                const string jsonStart = "```json";
                const string jsonEnd = "```";

                var si = html.IndexOf(jsonStart, index);
                if (si < 0)
                {
                    await VS.MessageBox.ShowWarningAsync(
                        string.Format(
                            FreeAIr.Resources.Resources.Docker_image_for__0__MCP_server_installed,
                            serverName
                            )
                        );
                    return (null, null);
                }

                var ei = html.IndexOf(jsonEnd, si + jsonStart.Length);
                if (ei < 0)
                {
                    await VS.MessageBox.ShowWarningAsync(
                        string.Format(
                            FreeAIr.Resources.Resources.Docker_image_for__0__MCP_server_installed,
                            serverName
                            )
                        );
                    return (null, null);
                }

                var json = html
                    .Substring(si + jsonStart.Length, ei - si - jsonStart.Length - jsonEnd.Length + 2)
                    ;

                var mcpServers = System.Text.Json.JsonSerializer.Deserialize<DockerMcpServers>(json);
                if (mcpServers.Servers.Count != 1)
                {
                    await VS.MessageBox.ShowWarningAsync(
                        string.Format(
                            FreeAIr.Resources.Resources.Docker_image_for__0__MCP_server_installed,
                            serverName
                            )
                        );
                    return (null, null);
                }

                var pair = mcpServers.Convert().First();
                return pair;
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }

            return (null, null);
        }

        /// <summary>
        /// Pages through the Docker Hub `mcp` repository listing, fetching every published MCP
        /// server image into <see cref="_serverList"/>, then refreshes the filtered list.
        /// Network/parse failures for a page stop the pagination but do not throw.
        /// </summary>
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
                    excp.ActivityLogException();

                    nextUrl = null;
                }
            }
            while (!string.IsNullOrEmpty(nextUrl));

            Refilter();
        }

        /// <summary>
        /// Rebuilds <see cref="FilteredServerList"/> from <see cref="_serverList"/>, keeping only
        /// servers whose name or description contains the current <see cref="Filter"/> text
        /// (case-insensitive).
        /// </summary>
        private void Refilter()
        {
            var lfilter = Filter?.ToLower();

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


        /// <summary>
        /// JSON converter that reads a `mcpServers` object's per-server values as raw JSON text
        /// (rather than a fixed shape) and writes them back out as embedded JSON, since each
        /// server's configuration shape varies (stdio vs http).
        /// </summary>
        private sealed class DictionaryToStringConverter : JsonConverter<Dictionary<string, string>>
        {
            /// <summary>
            /// Reads a JSON object, capturing each property's raw JSON text as the dictionary value.
            /// </summary>
            public override Dictionary<string, string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                var root = doc.RootElement;

                var result = new Dictionary<string, string>();

                foreach (var property in root.EnumerateObject())
                {
                    // Сериализуем значение свойства в строку
                    result[property.Name] = property.Value.GetRawText();
                }

                return result;
            }

            /// <summary>
            /// Writes the dictionary back out as a JSON object, re-parsing each stored raw JSON
            /// string value and embedding it under its property name.
            /// </summary>
            public override void Write(Utf8JsonWriter writer, Dictionary<string, string> value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                foreach (var kvp in value)
                {
                    // Десериализуем строку обратно в JSON
                    using var doc = JsonDocument.Parse(kvp.Value);
                    writer.WritePropertyName(kvp.Key);
                    doc.WriteTo(writer);
                }

                writer.WriteEndObject();
            }
        }

        /// <summary>
        /// DTO matching the `{"mcpServers": {...}}` JSON block found in a Docker MCP image's
        /// "Use this MCP Server" description, used to extract the server's install configuration.
        /// </summary>
        private sealed class DockerMcpServers
        {
            /// <summary>
            /// Raw per-server JSON configuration text, keyed by server name.
            /// </summary>
            [JsonPropertyName("mcpServers")]
            [JsonConverter(typeof(DictionaryToStringConverter))]
            public Dictionary<string, string> Servers
            {
                get; set;
            }

            /// <summary>
            /// Converts the raw server entries into name/<see cref="McpServer"/> pairs, currently
            /// always treating them as stdio servers.
            /// </summary>
            public IEnumerable<(string, McpServer)> Convert()
            {
                foreach (var pair in Servers)
                {
                    //var heuristicType = pair.Value.Contains("\"command\"")
                    //    ? McpServerType.Stdio
                    //    : McpServerType.Http
                    //    ;

                    yield return (pair.Key, new McpServer(McpServerType.Stdio, pair.Value));
                }
            }
        }


        /// <summary>
        /// A single Docker Hub MCP server image entry shown in the search list, with a command
        /// to open its Docker Hub page in the browser.
        /// </summary>
        public sealed class DockerMcpServerInfo : BaseViewModel
        {
            /// <summary>
            /// Backing field for the lazily created <see cref="OpenBrowserCommand"/>.
            /// </summary>
            private ICommand _openBrowserCommand;

            /// <summary>
            /// The MCP server's Docker Hub image name (under the `mcp` namespace).
            /// </summary>
            public string ServerName
            {
                get;
            }

            /// <summary>
            /// The short description of the server shown in the search list.
            /// </summary>
            public string ServerDescription
            {
                get;
            }

            /// <summary>
            /// Command that opens this server's Docker Hub page in the default browser.
            /// </summary>
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

            /// <summary>
            /// Creates a server list entry with the given Docker Hub image name and description.
            /// </summary>
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

    /// <summary>
    /// DTO for a Docker Hub repository/image record, as returned by the Docker Hub v2 API for a
    /// single MCP server image (e.g. `mcp/&lt;server-name&gt;`).
    /// </summary>
    public class Image
    {
        /// <summary>Docker Hub username of the image owner.</summary>
        public string user
        {
            get; set;
        }
        /// <summary>The image's repository name, e.g. the MCP server name.</summary>
        public string name
        {
            get; set;
        }
        /// <summary>The Docker Hub namespace the image is published under (`mcp`).</summary>
        public string _namespace
        {
            get; set;
        }
        /// <summary>Docker Hub's repository type classification for the image.</summary>
        public object repository_type
        {
            get; set;
        }
        /// <summary>Docker Hub status code of the repository.</summary>
        public int status
        {
            get; set;
        }
        /// <summary>Human-readable description of <see cref="status"/>.</summary>
        public string status_description
        {
            get; set;
        }
        /// <summary>Short description of the image shown in search results.</summary>
        public string description
        {
            get; set;
        }
        /// <summary>Whether the repository is private.</summary>
        public bool is_private
        {
            get; set;
        }
        /// <summary>Whether the image is built by Docker Hub's automated build system.</summary>
        public bool is_automated
        {
            get; set;
        }
        /// <summary>Number of Docker Hub stars the repository has.</summary>
        public int star_count
        {
            get; set;
        }
        /// <summary>Number of times the image has been pulled.</summary>
        public int pull_count
        {
            get; set;
        }
        /// <summary>Timestamp of the repository's last update.</summary>
        public DateTime last_updated
        {
            get; set;
        }
        /// <summary>Timestamp of the repository's last modification.</summary>
        public DateTime last_modified
        {
            get; set;
        }
        /// <summary>Timestamp the repository was registered on Docker Hub.</summary>
        public DateTime date_registered
        {
            get; set;
        }
        /// <summary>Number of collaborators with access to the repository.</summary>
        public int collaborator_count
        {
            get; set;
        }
        /// <summary>Docker Hub organization/team affiliation data for the repository.</summary>
        public object affiliation
        {
            get; set;
        }
        /// <summary>Docker Hub username of the repository's hub user.</summary>
        public string hub_user
        {
            get; set;
        }
        /// <summary>Whether the current API caller has starred the repository.</summary>
        public bool has_starred
        {
            get; set;
        }
        /// <summary>
        /// The repository's full markdown description; this is where the MCP server's
        /// "Use this MCP Server" JSON configuration snippet is parsed from by <see cref="SearchForDockerMcpServerViewModel.ConstructServerAsync"/>.
        /// </summary>
        public string full_description
        {
            get; set;
        }
        /// <summary>The caller's read/write/admin permissions on the repository.</summary>
        public Permissions permissions
        {
            get; set;
        }
        /// <summary>Media types associated with the repository's content.</summary>
        public string[] media_types
        {
            get; set;
        }
        /// <summary>Content types associated with the repository.</summary>
        public string[] content_types
        {
            get; set;
        }
        /// <summary>Docker Hub categories the repository is tagged with.</summary>
        public Category[] categories
        {
            get; set;
        }
        /// <summary>Immutable-tags policy configured for the repository.</summary>
        public Immutable_Tags_Settings immutable_tags_settings
        {
            get; set;
        }
        /// <summary>Total storage size of the repository's image layers, in bytes, if known.</summary>
        public long? storage_size
        {
            get; set;
        }
    }

    /// <summary>
    /// DTO for the caller's access permissions on a Docker Hub repository.
    /// </summary>
    public class Permissions
    {
        /// <summary>Whether the caller can read/pull the repository.</summary>
        public bool read
        {
            get; set;
        }
        /// <summary>Whether the caller can write/push to the repository.</summary>
        public bool write
        {
            get; set;
        }
        /// <summary>Whether the caller has admin rights on the repository.</summary>
        public bool admin
        {
            get; set;
        }
    }

    /// <summary>
    /// DTO for a Docker Hub repository's immutable-tags protection settings.
    /// </summary>
    public class Immutable_Tags_Settings
    {
        /// <summary>Whether immutable-tags protection is enabled for the repository.</summary>
        public bool enabled
        {
            get; set;
        }
        /// <summary>The tag-matching rules the immutable-tags protection applies to.</summary>
        public string[] rules
        {
            get; set;
        }
    }

    /// <summary>
    /// DTO for one page of the Docker Hub repository listing (e.g. all images under the `mcp`
    /// publisher), as returned by the paginated Docker Hub v2 API.
    /// </summary>
    public class ImagesFromPublisher
    {
        /// <summary>Total number of repositories across all pages.</summary>
        public int count
        {
            get; set;
        }
        /// <summary>URL of the next page of results, or null if this is the last page.</summary>
        public string next
        {
            get; set;
        }
        /// <summary>URL of the previous page of results, if any.</summary>
        public object previous
        {
            get; set;
        }
        /// <summary>The repository/image records on this page.</summary>
        public Image[] results
        {
            get; set;
        }
    }

    /// <summary>
    /// DTO for a Docker Hub category tag attached to a repository.
    /// </summary>
    public class Category
    {
        /// <summary>Display name of the category.</summary>
        public string name
        {
            get; set;
        }
        /// <summary>URL-safe slug identifying the category.</summary>
        public string slug
        {
            get; set;
        }
    }

    #endregion
}
