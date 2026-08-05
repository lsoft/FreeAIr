using FreeAIr.Helper;
using FreeAIr.Seaarch;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy.VS.Tools
{
    /// <summary>
    /// MCP tool that lets the chat model run a Google web search and get back the titles, links and
    /// snippets of the results, for questions the model cannot answer from the solution alone.
    /// </summary>
    public sealed class WebSearchTool : VisualStudioMcpServerTool
    {
        /// <summary>
        /// The single shared instance of this tool, registered by <see cref="VisualStudioMcpServerProxy"/>.
        /// </summary>
        public static readonly WebSearchTool Instance = new();

        /// <summary>
        /// The tool name advertised to the chat model for the web search operation.
        /// </summary>
        public const string VisualStudioToolName = "WebSearch";

        /// <summary>
        /// JSON schema key for the term to search for.
        /// </summary>
        private const string SearchTermParameterName = "search_term";

        /// <summary>
        /// Declares the tool's name and JSON schema describing the required search_term parameter.
        /// </summary>
        public WebSearchTool(
            ) : base(
                VisualStudioMcpServerProxy.VisualStudioProxyName,
                VisualStudioToolName,
                "Executes a web search for a given term. Returns a JSON-formatted list of found entries.",
                $$$"""
                {
                    "type": "object",
                    "properties": {
                        "{{{SearchTermParameterName}}}": {
                            "type": "string",
                            "description": "A search term"
                            }
                        },
                    "required": ["{{{SearchTermParameterName}}}"]
                }
                """)
        {
        }

        /// <summary>
        /// Runs a Google search for the given term via <see cref="GoogleSearcher"/> and serializes
        /// the top results into a JSON list of title/details/link entries.
        /// </summary>
        public override async Task<McpServerProxyToolCallResult?> CallToolAsync(
            string toolName,
            IReadOnlyDictionary<string, object?>? arguments = null,
            CancellationToken cancellationToken = default
            )
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (!arguments.TryGetValue(SearchTermParameterName, out var searchTermParameterName))
                {
                    return McpServerProxyToolCallResult.CreateFailed($"Parameter {SearchTermParameterName} does not found.");
                }
                var searchTerm = searchTermParameterName as string;

                var searcher = new GoogleSearcher(
                    pageImagesFolder: null
                    );
                var results = await searcher.SearchAsync(
                    searchTerm,
                    20
                    );
                if (results is null || results.Count == 0)
                {
                    return McpServerProxyToolCallResult.CreateFailed("Search failed.");
                }

                var searchResults = new SearchResults
                {
                    FoundResults = results.ConvertAll(r => new SearchResult
                    {
                        Details = r.Details,
                        Link = r.Link,
                        Title = r.Title,
                    })
                };

                var result = JsonSerializer.Serialize(searchResults);

                return McpServerProxyToolCallResult.CreateSuccess(result);
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();

                return McpServerProxyToolCallResult.CreateFailed("Search failed.");
            }
        }

        /// <summary>
        /// The JSON payload returned to the chat model for a WebSearch call.
        /// </summary>
        public sealed class SearchResults
        {

            /// <summary>The search results found, in the order the search engine returned them.</summary>
            public List<SearchResult> FoundResults
            {
                get;
                set;
            }
        }

        /// <summary>
        /// One entry of a web search result: its title, description snippet and source link.
        /// </summary>
        public sealed class SearchResult
        {
            /// <summary>
            /// Заголовок пункта из выдачи поисковой системы.
            /// </summary>
            public string Title
            {
                get;
                set;
            }

            /// <summary>
            /// Описание пункта из выдачи поисковой системы.
            /// </summary>
            public string Details
            {
                get;
                set;
            }


            /// <summary>
            /// Ссылка на источник пункта из выдачи поисковой системы.
            /// </summary>
            public string Link
            {
                get;
                set;
            }
        }

    }



}
