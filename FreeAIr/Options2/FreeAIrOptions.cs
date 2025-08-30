using Dto;
using FreeAIr.Helper;
using FreeAIr.MCP.McpServerProxy;
using FreeAIr.Options2.Agent;
using FreeAIr.Options2.Mcp;
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
    public sealed partial class FreeAIrOptions : ICloneable
    {
        #region static fields and constructor

        private static readonly JsonSerializerOptions _readOptions;
        private static readonly JsonSerializerOptions _writeOptions;

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

        public UnsortedJson Unsorted
        {
            get;
            set;
        }

        public AgentCollectionJson AgentCollection
        {
            get;
            set;
        }

        public McpServers AvailableMcpServers
        {
            get;
            set;
        }

        public AvailableMcpServersJson AvailableTools
        {
            get;
            set;
        }

        public SupportCollectionJson Supports
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
            };
        }


        #region deserialize and related

        public static async Task<AvailableMcpServersJson> DeserializeAvailableToolsAsync()
        {
            var options = await DeserializeAsync(null);
            return options.AvailableTools;
        }

        public static async Task<McpServers> DeserializeMcpServersAsync()
        {
            var options = await DeserializeAsync(null);
            return options.AvailableMcpServers;
        }

        public static async Task<UnsortedJson> DeserializeUnsortedAsync()
        {
            var options = await DeserializeAsync(null);
            return options.Unsorted;
        }

        public static async Task<AgentCollectionJson> DeserializeAgentCollectionAsync()
        {
            var options = await DeserializeAsync(null);
            return options.AgentCollection;
        }

        public static async Task<AgentJson?> DeserializeAgentByNameAsync(
            string agentName
            )
        {
            var options = await DeserializeAsync(null);
            var agents = options.AgentCollection.Agents;
            var filteredAgents = agents.FindAll(a => !string.IsNullOrEmpty(a.Technical.GetToken()));
            if (filteredAgents.Count == 0)
            {
                return null;
            }

            var result = filteredAgents.FirstOrDefault(
                a => a.Name == agentName
                );
            return result;
        }

        public static async Task<SupportCollectionJson> DeserializeSupportCollectionAsync()
        {
            var options = await DeserializeAsync(null);
            return options.Supports;
        }

        public static async Task<List<SupportActionJson>> DeserializeSupportActionsAsync(
            Func<SupportActionJson, bool> filter
            )
        {
            var options = await DeserializeAsync(null);
            return options.Supports.Actions.FindAll(a => filter(a));
        }

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
                        if (fileResult is not null
                            || (place.HasValue && place.Value == OptionsPlaceEnum.SolutionRelatedFilePath)
                            )
                        {
                            return fileResult;
                        }
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
                    if (vsResult is not null
                        || (place.HasValue && place.Value == OptionsPlaceEnum.VisualStudioOption)
                        )
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

        public async Task<OptionsPlaceEnum> SerializeAsync(
            OptionsPlaceEnum? place
            )
        {
            //serialize to file
            var filePath = await ComposeOptionsFilePathAsync();
            if ((!place.HasValue && File.Exists(filePath)) || place == OptionsPlaceEnum.SolutionRelatedFilePath)
            {
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

        public static Task<string?> ComposeOptionsFilePathAsync(
            )
        {
            return ComposeFilePathAsync("options");
        }

        public static Task<string?> ComposeEmbeddingsFilePathAsync(
            )
        {
            return ComposeFilePathAsync("embeddings");
        }

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
        SolutionRelatedFilePath,
        VisualStudioOption
    }


    public static class OptionsPlaceHelper
    {
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
