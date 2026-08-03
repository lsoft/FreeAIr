using EnvDTE80;
using FreeAIr.Grep;
using FreeAIr.Helper;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy.VS.Tools
{
    /// <summary>
    /// Grep for the open solution: a pattern goes in, the matching lines with their files and line
    /// numbers come out.
    ///
    /// Nothing is installed and no process is started - the matching is
    /// <see cref="FreeAIr.Grep.GrepEngine"/>, which lives in the RAG assembly and is covered by its
    /// tests. Running inside devenv buys two things a `grep.exe` could not have given: the text of
    /// the files which are open and not saved yet, and the option to search the files of the
    /// solution rather than everything on the disk.
    /// </summary>
    public sealed class SearchFileContentTool : VisualStudioMcpServerTool
    {
        /// <summary>
        /// Files bigger than this are not searched. A source file never comes close; the things
        /// which do are the logs, the dumps and the data sets, and reading them would cost more
        /// than the whole rest of the scan.
        /// </summary>
        private const long MaxFileSizeBytes = 5L * 1024 * 1024;

        private const int MaxAllowedResultCount = 500;
        private const int MaxAllowedContextLineCount = 5;

        private const string PatternParameterName = "pattern";
        private const string IsRegularExpressionParameterName = "is_regular_expression";
        private const string CaseSensitiveParameterName = "case_sensitive";
        private const string WholeWordParameterName = "whole_word";
        private const string InvertMatchParameterName = "invert_match";
        private const string FileMaskParameterName = "file_mask";
        private const string SearchScopeParameterName = "search_scope";
        private const string SubfolderParameterName = "subfolder";
        private const string ContextLineCountParameterName = "context_line_count";
        private const string MaxResultCountParameterName = "max_result_count";

        private const string SolutionItemsScopeValue = "solution_items";
        private const string AllFilesScopeValue = "all_files";

        public static readonly SearchFileContentTool Instance = new();

        public const string VisualStudioToolName = "SearchFileContent";

        public SearchFileContentTool(
            ) : base(
                VisualStudioMcpServerProxy.VisualStudioProxyName,
                VisualStudioToolName,
                "Searches the text of the files of the open solution, like the grep utility does. Returns a JSON-formatted list of matching lines, each with the file path relative to the solution and the line number. Use this function to find where an identifier, a string literal, a setting or any other text occurs. Prefer it over reading files one by one.",
                $$$"""
                {
                    "type": "object",
                    "properties": {
                        "{{{PatternParameterName}}}": {
                            "type": "string",
                            "description": "The text to look for. A plain text by default; a .NET regular expression when is_regular_expression is true."
                            },
                        "{{{IsRegularExpressionParameterName}}}": {
                            "type": "boolean",
                            "description": "Treat the pattern as a .NET regular expression. Leave this empty to search for a plain text."
                            },
                        "{{{CaseSensitiveParameterName}}}": {
                            "type": "boolean",
                            "description": "Match the case of the pattern. Leave this empty to ignore the case."
                            },
                        "{{{WholeWordParameterName}}}": {
                            "type": "boolean",
                            "description": "Match whole words only, so that 'Chat' does not match 'ChatWindow'. Leave this empty to match anywhere."
                            },
                        "{{{InvertMatchParameterName}}}": {
                            "type": "boolean",
                            "description": "Return the lines which do NOT match the pattern, like 'grep -v' does. Leave this empty to return the matching lines. Narrow the search down with file_mask or subfolder when you use this, because almost every line of a solution does not match almost any pattern."
                            },
                        "{{{FileMaskParameterName}}}": {
                            "type": "string",
                            "description": "Semicolon separated list of file masks, for example '*.cs;*.xaml'. A mask which contains a directory separator is matched against the path relative to the solution, otherwise against the file name. A mask which starts with '!' excludes instead of including, for example '*.cs;!*.Designer.cs;!*.g.cs' searches the C# files except the generated ones. Leave this empty to search every file."
                            },
                        "{{{SearchScopeParameterName}}}": {
                            "type": "string",
                            "enum": ["{{{SolutionItemsScopeValue}}}", "{{{AllFilesScopeValue}}}"],
                            "description": "Which files to search: 'solution_items' (the default) searches the files included into the projects of the solution, 'all_files' searches every file in the solution folder, including the ones which are not part of any project. Build output, version control and package folders are skipped either way."
                            },
                        "{{{SubfolderParameterName}}}": {
                            "type": "string",
                            "description": "Narrows the search down to this folder, relative to the folder of the solution. Leave this empty to search the whole solution."
                            },
                        "{{{ContextLineCountParameterName}}}": {
                            "type": "integer",
                            "description": "How many lines above and below a match to return with it, from 0 to 5. Leave this empty to return the matching lines alone."
                            },
                        "{{{MaxResultCountParameterName}}}": {
                            "type": "integer",
                            "description": "The most matching lines to return, up to 500. Defaults to 100. The answer says whether this limit has been reached."
                            }
                        },
                    "required": ["{{{PatternParameterName}}}"]
                }
                """
                )
        {
        }

        public override async Task<McpServerProxyToolCallResult?> CallToolAsync(
            string toolName,
            IReadOnlyDictionary<string, object?>? arguments = null,
            CancellationToken cancellationToken = default
            )
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (arguments is null || !arguments.TryGetValue(PatternParameterName, out var patternObj))
            {
                return McpServerProxyToolCallResult.CreateFailed($"Parameter {PatternParameterName} does not found.");
            }

            var pattern = patternObj as string;
            if (string.IsNullOrEmpty(pattern))
            {
                return McpServerProxyToolCallResult.CreateFailed($"Parameter {PatternParameterName} is empty.");
            }

            var options = new GrepOptions
            {
                Pattern = pattern,
                UseRegularExpression = ReadBoolean(arguments, IsRegularExpressionParameterName),
                CaseSensitive = ReadBoolean(arguments, CaseSensitiveParameterName),
                WholeWord = ReadBoolean(arguments, WholeWordParameterName),
                InvertMatch = ReadBoolean(arguments, InvertMatchParameterName),
                ContextLineCount = Clamp(
                    ReadInteger(arguments, ContextLineCountParameterName, 0),
                    0,
                    MaxAllowedContextLineCount
                    ),
                MaxMatchCount = Clamp(
                    ReadInteger(arguments, MaxResultCountParameterName, 100),
                    1,
                    MaxAllowedResultCount
                    )
            };

            GrepEngine engine;
            try
            {
                engine = new GrepEngine(options);
            }
            catch (ArgumentException excp)
            {
                //a pattern the model has written itself; telling it what is wrong is what lets it
                //write the next one correctly
                return McpServerProxyToolCallResult.CreateFailed(
                    $"Parameter {PatternParameterName} is not a valid regular expression: {excp.Message}"
                    );
            }

            var solution = await Community.VisualStudio.Toolkit.VS.Solutions.GetCurrentSolutionAsync();
            if (solution is null || string.IsNullOrEmpty(solution.FullPath))
            {
                return McpServerProxyToolCallResult.CreateFailed("There is no open solution to search in.");
            }

            var solutionFilePath = solution.FullPath;
            var rootFolder = Path.GetDirectoryName(solutionFilePath);

            var searchFolder = rootFolder;
            var subfolder = ReadString(arguments, SubfolderParameterName);
            if (!string.IsNullOrEmpty(subfolder))
            {
                searchFolder = PathHelper.GetFullPath(rootFolder, subfolder);

                if (!IsUnderFolder(searchFolder, rootFolder))
                {
                    return McpServerProxyToolCallResult.CreateFailed(
                        $"Parameter {SubfolderParameterName} points outside of the solution folder."
                        );
                }

                if (!Directory.Exists(searchFolder))
                {
                    return McpServerProxyToolCallResult.CreateFailed(
                        $"Folder {subfolder} does not exist in current solution."
                        );
                }
            }

            var scope = ReadString(arguments, SearchScopeParameterName);
            var searchAllFiles = StringComparer.InvariantCultureIgnoreCase.Compare(scope, AllFilesScopeValue) == 0;

            List<string>? solutionFiles = null;
            if (!searchAllFiles)
            {
                var items = await solution.ProcessDownRecursivelyForAsync(
                    item => !item.IsNonVisibleItem,
                    false,
                    cancellationToken
                    );

                solutionFiles = items
                    .Select(i => i.SolutionItem.FullPath)
                    .Where(p => !string.IsNullOrEmpty(p))
                    .Distinct(StringComparer.InvariantCultureIgnoreCase)
                    .ToList();
            }

            //the files which are open and modified are searched as the user sees them, not as they
            //are on the disk. This is the whole reason the tool runs inside devenv, and it has to
            //be collected here, on the main thread, before the scan moves off it
            var unsavedDocuments = await CollectUnsavedDocumentsAsync();

            var mask = FileMask.Parse(
                ReadString(arguments, FileMaskParameterName)
                );

            //a scan of thousands of files on the main thread is a frozen IDE
            var result = await Task.Run(
                () => Search(
                    engine,
                    solutionFilePath,
                    searchFolder,
                    //the walk already starts at the folder, and the items of a solution are not
                    //bound to lie under it at all: a `.sln` in a subfolder of the repository is a
                    //normal layout, and filtering by the folder would answer nothing there
                    string.IsNullOrEmpty(subfolder) ? null : searchFolder,
                    solutionFiles,
                    mask,
                    unsavedDocuments,
                    cancellationToken
                    ),
                cancellationToken
                );

            var answer = new SearchResultJson
            {
                Pattern = pattern,
                InvertMatch = options.InvertMatch,
                SearchScope = searchAllFiles ? AllFilesScopeValue : SolutionItemsScopeValue,
                SearchedFolder = string.IsNullOrEmpty(subfolder) ? "." : subfolder,
                FilesScanned = result.FilesScanned,
                FilesWithMatches = result.FilesWithMatches,
                MatchCount = result.Matches.Count,
                LimitReached = result.LimitReached,
                TimedOutFileCount = result.TimedOutFileCount,
                Matches = result.Matches
                    .Select(m => new MatchJson
                    {
                        RelativePath = m.RelativePath,
                        LineNumber = m.LineNumber,
                        Line = m.Line,
                        ContextBefore = m.ContextBefore.Count > 0 ? m.ContextBefore.ToArray() : null,
                        ContextAfter = m.ContextAfter.Count > 0 ? m.ContextAfter.ToArray() : null
                    })
                    .ToArray()
            };

            return McpServerProxyToolCallResult.CreateSuccess(
                JsonSerializer.Serialize(answer)
                );
        }

        private static GrepSearchResult Search(
            GrepEngine engine,
            string solutionFilePath,
            string searchFolder,
            string? restrictToFolder,
            IReadOnlyList<string>? solutionFiles,
            FileMask mask,
            IReadOnlyDictionary<string, string> unsavedDocuments,
            CancellationToken cancellationToken
            )
        {
            var result = engine.CreateResult();

            var files = solutionFiles ?? (IEnumerable<string>)FileWalker.EnumerateFiles(
                searchFolder,
                cancellationToken
                );

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                //the list of solution items has not been narrowed by the subfolder yet; the walker
                //has been started at it already, so checking it again there costs nothing
                if (restrictToFolder is not null && !IsUnderFolder(file, restrictToFolder))
                {
                    continue;
                }

                if (TextFile.HasBinaryExtension(file))
                {
                    continue;
                }

                var relativePath = file.MakeRelativeAgainst(solutionFilePath);

                if (!mask.IsMatch(relativePath))
                {
                    continue;
                }

                if (!unsavedDocuments.TryGetValue(file, out var text))
                {
                    if (!TryReadFile(file, out text))
                    {
                        continue;
                    }
                }

                if (!engine.SearchIn(relativePath, text, result))
                {
                    break;
                }
            }

            return result;
        }

        private static bool TryReadFile(
            string filePath,
            out string text
            )
        {
            text = string.Empty;

            try
            {
                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists || fileInfo.Length > MaxFileSizeBytes)
                {
                    return false;
                }

                return TextFile.TryDecode(
                    File.ReadAllBytes(filePath),
                    out text
                    );
            }
            catch (Exception excp) when (excp is IOException || excp is UnauthorizedAccessException)
            {
                //a file which is locked by another process is not worth failing the whole search for
                return false;
            }
        }

        /// <summary>
        /// The text of every document which is open in the editor and has unsaved changes, keyed by
        /// its path. There are a few of them at most, so asking for each buffer is cheap - asking
        /// for every file of the solution would not have been.
        /// </summary>
        private static async Task<Dictionary<string, string>> CollectUnsavedDocumentsAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var result = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            var dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
            if (dte is null)
            {
                return result;
            }

            foreach (EnvDTE.Document document in dte.Documents)
            {
                string fullName;
                try
                {
                    if (document.Saved)
                    {
                        continue;
                    }

                    fullName = document.FullName;
                }
                catch (Exception)
                {
                    //not every window in the list is a file on the disk
                    continue;
                }

                if (string.IsNullOrEmpty(fullName))
                {
                    continue;
                }

                var documentView = await Community.VisualStudio.Toolkit.VS.Documents.GetDocumentViewAsync(fullName);
                if (documentView?.Document is null)
                {
                    continue;
                }

                result[fullName] = documentView.Document.TextBuffer.CurrentSnapshot.GetText();
            }

            return result;
        }

        private static bool IsUnderFolder(
            string path,
            string folder
            )
        {
            var normalizedFolder = folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar
                ;

            return path.StartsWith(normalizedFolder, StringComparison.InvariantCultureIgnoreCase);
        }

        private static string? ReadString(
            IReadOnlyDictionary<string, object?> arguments,
            string parameterName
            )
        {
            if (!arguments.TryGetValue(parameterName, out var value))
            {
                return null;
            }

            return value as string;
        }

        /// <summary>
        /// A boolean which a model may well have spelled as a string. The schema says `boolean`,
        /// but not every provider makes the model obey it, and a refused tool call over the word
        /// "true" helps nobody.
        /// </summary>
        private static bool ReadBoolean(
            IReadOnlyDictionary<string, object?> arguments,
            string parameterName
            )
        {
            if (!arguments.TryGetValue(parameterName, out var value) || value is null)
            {
                return false;
            }

            if (value is bool boolValue)
            {
                return boolValue;
            }

            if (value is string stringValue && bool.TryParse(stringValue, out var parsed))
            {
                return parsed;
            }

            return false;
        }

        private static int ReadInteger(
            IReadOnlyDictionary<string, object?> arguments,
            string parameterName,
            int defaultValue
            )
        {
            if (!arguments.TryGetValue(parameterName, out var value) || value is null)
            {
                return defaultValue;
            }

            switch (value)
            {
                case int intValue:
                    return intValue;
                case long longValue:
                    return (int)longValue;
                case double doubleValue:
                    return (int)doubleValue;
                case string stringValue when int.TryParse(stringValue, out var parsed):
                    return parsed;
                default:
                    return defaultValue;
            }
        }

        private static int Clamp(
            int value,
            int min,
            int max
            )
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        private sealed class SearchResultJson
        {
            public string Pattern
            {
                get;
                set;
            }

            /// <summary>
            /// Echoed back because the lines below mean the opposite when it is set, and a list of
            /// lines which do not match reads exactly like a list of lines which do.
            /// </summary>
            public bool InvertMatch
            {
                get;
                set;
            }

            public string SearchScope
            {
                get;
                set;
            }

            public string SearchedFolder
            {
                get;
                set;
            }

            public int FilesScanned
            {
                get;
                set;
            }

            public int FilesWithMatches
            {
                get;
                set;
            }

            public int MatchCount
            {
                get;
                set;
            }

            /// <summary>
            /// True when there are more matches than were returned. The model has to be told, or it
            /// will read a truncated list as the whole truth.
            /// </summary>
            public bool LimitReached
            {
                get;
                set;
            }

            public int TimedOutFileCount
            {
                get;
                set;
            }

            public MatchJson[] Matches
            {
                get;
                set;
            }
        }

        private sealed class MatchJson
        {
            public string RelativePath
            {
                get;
                set;
            }

            public int LineNumber
            {
                get;
                set;
            }

            public string Line
            {
                get;
                set;
            }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string[]? ContextBefore
            {
                get;
                set;
            }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string[]? ContextAfter
            {
                get;
                set;
            }
        }
    }
}
