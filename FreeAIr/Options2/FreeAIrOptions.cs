using Dto;
using FreeAIr.Helper;
using FreeAIr.MCP.McpServerProxy;
using FreeAIr.Options2.Agent;
using FreeAIr.Options2.Mcp;
using FreeAIr.Options2.Rag;
using FreeAIr.Options2.Support;
using FreeAIr.Options2.Unsorted;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using FreeAIr.BLogic;

namespace FreeAIr.Options2
{
    /// <summary>
    /// The root of the FreeAIr json settings: agents, MCP servers and their tools, support actions
    /// and the unsorted knobs. These settings are shared by the whole team, as opposed to the
    /// per-user Visual Studio option pages (`FreeAIr.Options`).
    ///
    /// The settings live in one of two places, see <see cref="OptionsPlaceEnum"/>:
    /// the solution-related file `.freeair\&lt;solution name&gt;_options.json` (preferred, meant to be
    /// committed) or the Visual Studio option store. When both exist, the file wins.
    ///
    /// Reads are cheap: they go through <see cref="DataPieceCache"/> and re-parse only when the
    /// file timestamp (or the stored string) changes, so the `Deserialize*Async` shortcuts may be
    /// called freely from hot paths.
    /// </summary>
    public sealed partial class FreeAIrOptions : ICloneable
    {
        #region static fields and constructor

        private static readonly JsonSerializerOptions _readOptions;
        private static readonly JsonSerializerOptions _writeOptions;

        /// <summary>
        /// Builds the two serializer configurations, which differ on purpose: reading skips comments
        /// so a hand edited settings file may be annotated, and writing indents and leaves non ASCII
        /// unescaped so prompts written in any language stay readable in the file.
        /// </summary>
        static FreeAIrOptions()
        {
            _readOptions = new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip
            };
            _readOptions.Converters.Add(new StringEnumConverter<SupportScopeEnum>());

            _writeOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };
            _writeOptions.Converters.Add(new StringEnumConverter<SupportScopeEnum>());
        }

        #endregion

        /// <summary>The knobs which have not earned a section of their own: timeouts, limits, feature switches.</summary>
        public UnsortedJson Unsorted
        {
            get;
            set;
        }

        /// <summary>The configured LLM endpoints. Everything that talks to a model picks one from here by name.</summary>
        public AgentCollectionJson AgentCollection
        {
            get;
            set;
        }

        /// <summary>
        /// How to launch the external MCP servers — the same shape Claude Desktop and the other MCP
        /// clients use, so an existing configuration can be pasted in as is.
        /// </summary>
        public McpServers AvailableMcpServers
        {
            get;
            set;
        }

        /// <summary>
        /// Which of the discovered tools the user has left enabled. Kept apart from
        /// <see cref="AvailableMcpServers"/> because that describes what exists and this records a
        /// decision about it.
        /// </summary>
        public AvailableMcpServersJson AvailableTools
        {
            get;
            set;
        }

        /// <summary>The prompts offered in the menus. See <see cref="SupportCollectionJson"/>.</summary>
        public SupportCollectionJson Supports
        {
            get;
            set;
        }

        /// <summary>Settings of the `Use RAG` natural language search, see <see cref="RagJson"/>.</summary>
        public RagJson Rag
        {
            get;
            set;
        }

        public FreeAIrOptions()
        {
            Unsorted = new();
            AgentCollection = new();
            AvailableMcpServers = new();
            AvailableTools = new();
            Supports = new();
            Rag = new();
        }

        public object Clone()
        {
            return new FreeAIrOptions
            {
                Unsorted = (UnsortedJson)Unsorted.Clone(),
                AgentCollection = (AgentCollectionJson)AgentCollection.Clone(),
                AvailableMcpServers = (McpServers)AvailableMcpServers.Clone(),
                AvailableTools = (AvailableMcpServersJson)AvailableTools.Clone(),
                Supports = (SupportCollectionJson)Supports.Clone(),
                Rag = (RagJson)Rag.Clone(),
            };
        }


        #region deserialize and related

        /// <summary>Shortcut to the enabled tool state alone. Cached like every other read.</summary>
        public static async Task<AvailableMcpServersJson> DeserializeAvailableToolsAsync()
        {
            var options = await DeserializeAsync(null);
            return options.AvailableTools;
        }

        /// <summary>Shortcut to the MCP server launch configuration alone.</summary>
        public static async Task<McpServers> DeserializeMcpServersAsync()
        {
            var options = await DeserializeAsync(null);
            return options.AvailableMcpServers;
        }

        /// <summary>Shortcut to the unsorted knobs. The most called of these, being read on the typing path.</summary>
        public static async Task<UnsortedJson> DeserializeUnsortedAsync()
        {
            var options = await DeserializeAsync(null);
            return options.Unsorted;
        }

        /// <summary>Shortcut to the RAG settings: the embedding agent, the thresholds, the index behaviour.</summary>
        public static async Task<RagJson> DeserializeRagAsync()
        {
            var options = await DeserializeAsync(null);
            return options.Rag;
        }

        /// <summary>Shortcut to the agent list, for the pickers which let the user choose one.</summary>
        public static async Task<AgentCollectionJson> DeserializeAgentCollectionAsync()
        {
            var options = await DeserializeAsync(null);
            return options.AgentCollection;
        }

        /// <summary>
        /// The agent stored under this name, or null when the settings no longer have one.
        ///
        /// Deliberately without the `has a token` filter the pickers apply. A local server wants no
        /// token at all, and the agent this returns has been chosen by something which already
        /// knows what it needs — the index knows which agent built it, a support action names its
        /// own. Hiding a named agent because it has no token used to send both of those looking for
        /// a substitute, and the substitute was whatever cloud agent happened to be configured:
        /// asking it for embeddings gets a flat HTTP 400 out of a chat endpoint.
        /// </summary>
        public static async Task<AgentJson?> DeserializeAgentByNameAsync(
            string agentName
            )
        {
            var options = await DeserializeAsync(null);

            return options.AgentCollection.Agents.FirstOrDefault(
                a => a.Name == agentName
                );
        }

        /// <summary>Shortcut to the whole set of support actions.</summary>
        public static async Task<SupportCollectionJson> DeserializeSupportCollectionAsync()
        {
            var options = await DeserializeAsync(null);
            return options.Supports;
        }

        /// <summary>
        /// The support actions matching a predicate — in practice, the ones whose scope fits the
        /// menu about to be shown.
        /// </summary>
        public static async Task<List<SupportActionJson>> DeserializeSupportActionsAsync(
            Func<SupportActionJson, bool> filter
            )
        {
            var options = await DeserializeAsync(null);
            return options.Supports.Actions.FindAll(a => filter(a));
        }

        /// <summary>
        /// Loads the settings.
        /// </summary>
        /// <param name="place">
        /// Where to read from. When null, the solution-related file is tried first and the Visual
        /// Studio option store is used as a fallback; when set, only that place is consulted.
        /// </param>
        /// <returns>
        /// The settings, or a brand new default instance if nothing could be read.
        /// This method never throws: a broken json is logged into the activity log and treated as
        /// an absent one, because settings are read from paths where a failure would be fatal.
        /// </returns>
        public static async Task<FreeAIrOptions> DeserializeAsync(
            OptionsPlaceEnum? place = null
            )
        {
            try
            {
                if (!place.HasValue || place.Value == OptionsPlaceEnum.SolutionRelatedFilePath)
                {
                    var filePath = await ComposeOptionsFilePathAsync();
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        var fileResult = await DataPieceCache.GetValueAsync<FreeAIrOptions>(
                            filePath,
                            (fp) => File.Exists(fp),
                            (fp) =>
                            {
                                var fileInfo = new FileInfo(fp);
                                var newSignature = fileInfo.LastWriteTimeUtc;
                                return newSignature;
                            },
                            async (fp) =>
                            {
                                if (!File.Exists(fp))
                                {
                                    return null;
                                }

                                using var fs = new FileStream(fp, FileMode.Open);
                                var options = await JsonSerializer.DeserializeAsync<FreeAIrOptions>(fs, _readOptions);
                                return options;
                            }
                            );
                        if (fileResult is not null)
                        {
                            return fileResult;
                        }
                    }

                    if (place.HasValue)
                    {
                        //the file was asked for by name, so falling back to the Visual Studio
                        //option store would answer a question nobody has asked
                        return new FreeAIrOptions();
                    }
                }
                //file does not exists

                if (!place.HasValue || place.Value == OptionsPlaceEnum.VisualStudioOption)
                {
                    //trying to load options from Visual Studio
                    var vsResult = await DataPieceCache.GetValueAsync<FreeAIrOptions>(
                        "::VSOptions", //just a key which cannot be a file path
                        (o) => !string.IsNullOrEmpty(InternalPage.Instance.Options),
                        (o) =>
                        {
                            return InternalPage.Instance.Options;
                        },
                        (o) =>
                        {
                            var result = DeserializeFromString(InternalPage.Instance.Options);
                            return Task.FromResult((ICloneable)result.Clone());
                        }
                        );
                    if (vsResult is not null)
                    {
                        return vsResult;
                    }
                }
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }

            //VS options is not set
            //just create the defaults
            return new FreeAIrOptions();
        }

        /// <summary>
        /// Non-throwing counterpart of <see cref="DeserializeFromString"/>. Use it when the json
        /// comes from a text box the user is still typing in, and an invalid json is a normal
        /// intermediate state rather than an error.
        /// </summary>
        public static bool TryDeserializeFromString(
            string optionsJson,
            out FreeAIrOptions? options,
            out string? errorMessage
            )
        {
            try
            {
                options = DeserializeFromString(optionsJson);
                errorMessage = null;
                return true;
            }
            catch(Exception excp)
            {
                options = null;
                errorMessage = excp.Message;
                return false;
            }
        }

        /// <summary>
        /// Parses settings out of a json string, throwing on anything malformed. Used by the
        /// settings editor when the user presses save, where a silent default would discard their
        /// work.
        /// </summary>
        public static FreeAIrOptions DeserializeFromString(
            string optionsJson
            )
        {
            if (optionsJson is null)
            {
                throw new ArgumentNullException(nameof(optionsJson));
            }

            return JsonSerializer.Deserialize<FreeAIrOptions>(optionsJson, _readOptions);
        }


        #endregion

        #region serialize and related

        /// <summary>
        /// Saves the settings and reports where they went.
        /// </summary>
        /// <param name="place">
        /// Where to save. When null the destination is chosen automatically: the solution-related
        /// file if it already exists, the Visual Studio option store otherwise. In other words,
        /// saving never creates the json file on its own — the user has to ask for it explicitly.
        /// </param>
        public async Task<OptionsPlaceEnum> SerializeAsync(
            OptionsPlaceEnum? place
            )
        {
            //serialize to file
            var filePath = await ComposeOptionsFilePathAsync();
            if ((!place.HasValue && File.Exists(filePath)) || place == OptionsPlaceEnum.SolutionRelatedFilePath)
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    //сюда попадаем только при явно запрошенном месте: без открытого решения
                    //пути у файла настроек попросту нет
                    throw new InvalidOperationException(
                        "Cannot save the options into a solution-related file: no solution is opened."
                        );
                }

                var fileInfo = new FileInfo(filePath);
                var directoryPath = fileInfo.Directory.FullName;
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                using var fs = new FileStream(filePath, FileMode.Create);
                await JsonSerializer.SerializeAsync(fs, this, _writeOptions);
                return OptionsPlaceEnum.SolutionRelatedFilePath;
            }

            //serialize to VS option
            InternalPage.Instance.Options = SerializeToString(this);
            await InternalPage.Instance.SaveAsync();
            return OptionsPlaceEnum.VisualStudioOption;
        }

        /// <summary>Renders the settings as indented json — what the settings editor shows and what the option store keeps.</summary>
        public static string SerializeToString(
            FreeAIrOptions options
            )
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            //serialize to VS option
            var result = JsonSerializer.Serialize(options, _writeOptions);
            return result;
        }

        /// <summary>
        /// Replaces the enabled tool state and saves. Re-reads the settings first, so a tool being
        /// toggled does not overwrite an unrelated change made in the meantime.
        /// </summary>
        public static async Task SaveExternalMCPToolsAsync(
            AvailableMcpServersJson tools
            )
        {
            if (tools is null)
            {
                throw new ArgumentNullException(nameof(tools));
            }

            var options = await FreeAIrOptions.DeserializeAsync(null);
            options.AvailableTools = tools;
            await options.SerializeAsync(null);
        }

        /// <summary>Replaces the agent list and saves, re-reading the rest of the settings first.</summary>
        public static async Task SaveAgentsAsync(
            AgentCollectionJson agentCollection
            )
        {
            if (agentCollection is null)
            {
                throw new ArgumentNullException(nameof(agentCollection));
            }

            var options = await FreeAIrOptions.DeserializeAsync(null);
            options.AgentCollection = agentCollection;
            await options.SerializeAsync(null);
        }

        #endregion

        /// <summary>
        /// Validates the MCP servers subnode by actually starting the servers it describes.
        /// Returns true only when every configured server came up; otherwise the user is told
        /// which ones failed and the caller is expected to abandon the save.
        /// </summary>
        public static async Task<bool> ApplyMcpServerNodeAsync(
            McpServers servers
            )
        {
            if (servers is null)
            {
                throw new ArgumentNullException(nameof(servers));
            }

            try
            {
                var setupResult = await McpServerProxyApplication.UpdateExternalServersAsync(
                    servers
                    );
                if (setupResult is null)
                {
                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        $"Invalid MCP servers json subnode. Fix json and try again."
                        );
                    return false;
                }

                var failedServerNames = new List<string>();
                foreach (var mcpServer in servers.Servers)
                {
                    if (setupResult.SuccessStartedMcpServers.All(a => a.Name != mcpServer.Key))
                    {
                        //этот сервер не был инициализирован по какой-то причине
                        failedServerNames.Add(mcpServer.Key);
                    }
                }
                if (failedServerNames.Count > 0)
                {
                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        $"Some MCP servers failed to start: {string.Join(",", failedServerNames)}. Changes did not saved."
                        );
                    return false;
                }

                return true;
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();

                await VS.MessageBox.ShowErrorAsync(
                    Resources.Resources.Error,
                    excp.Message
                    + Environment.NewLine
                    + excp.StackTrace
                    );
            }

            return false;
        }

        /// <summary>Path of the settings file, `.freeair\&lt;solution name&gt;_options.json`.</summary>
        public static Task<string?> ComposeOptionsFilePathAsync(
            )
        {
            return ComposeFilePathAsync("options");
        }

        /// <summary>
        /// Base path of the RAG index, `.freeair\&lt;solution name&gt;_embeddings.json`. The outlines and
        /// the vectors are stored beside it under related names.
        /// </summary>
        public static Task<string?> ComposeEmbeddingsFilePathAsync(
            )
        {
            return ComposeFilePathAsync("embeddings");
        }

        /// <summary>
        /// Builds the path of a solution-related FreeAIr file:
        /// `&lt;solution folder&gt;\.freeair\&lt;solution name&gt;_&lt;suffix&gt;.json`.
        /// Returns null when no solution is opened. The folder is not created here.
        /// </summary>
        public static async Task<string?> ComposeFilePathAsync(
            string suffix
            )
        {
            var solution = await VS.Solutions.GetCurrentSolutionAsync();
            if (solution is null)
            {
                return null;
            }

            var solutionFileInfo = new FileInfo(solution.Name);

            var solutionName = solution.Name;
            if (solutionFileInfo.Extension.Length > 0)
            {
                solutionName = solutionName.Substring(0, solutionName.Length - solutionFileInfo.Extension.Length);
            }

            var folderPath = System.IO.Path.Combine(
                solutionFileInfo.Directory.FullName,
                ".freeair"
                );
            //if (!System.IO.Directory.Exists(folderPath))
            //{
            //    System.IO.Directory.CreateDirectory(folderPath);
            //}

            var filePath = System.IO.Path.Combine(
                folderPath,
                $"{solutionName}_{suffix}.json"
                );
            return filePath;
        }
    }

    /// <summary>
    /// Where to store options.
    /// </summary>
    public enum OptionsPlaceEnum
    {
        /// <summary>
        /// `&lt;solution folder&gt;\.freeair\&lt;solution name&gt;_options.json`.
        /// Recommended: it can be committed and shared with the team.
        /// </summary>
        SolutionRelatedFilePath,

        /// <summary>
        /// Visual Studio's own option store. Used when creating a file next to the solution is
        /// undesirable; the settings then stay on this machine only.
        /// </summary>
        VisualStudioOption
    }


    /// <summary>
    /// Names the storage places for the settings window, which offers them as a dropdown.
    /// </summary>
    public static class OptionsPlaceHelper
    {
        /// <summary>
        /// What to call a place in the UI. Null means neither in particular — the settings actually
        /// in force, wherever they were read from.
        /// </summary>
        public static string GetTitle(this OptionsPlaceEnum? place)
        {
            if (!place.HasValue)
            {
                return "Active options";
            }

            switch (place.Value)
            {
                case OptionsPlaceEnum.SolutionRelatedFilePath:
                    return "Solution-related json file";
                case OptionsPlaceEnum.VisualStudioOption:
                    return "Visual Studio options";
            }

            throw new InvalidOperationException(place.Value.ToString());
        }
    }
}
