using FreeAIr.Helper;
using FreeAIr.Seaarch;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy.VS.Tools
{
    public sealed class WebSearchTool : VisualStudioMcpServerTool
    {
        public static readonly WebSearchTool Instance = new();

        public const string VisualStudioToolName = "WebSearch";

        private const string SearchTermParameterName = "search_term";

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

        public sealed class SearchResults
        {

            public List<SearchResult> FoundResults
            {
                get;
                set;
            }
        }

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
