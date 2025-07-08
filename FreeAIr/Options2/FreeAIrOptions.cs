using Dto;
using FreeAIr.Helper;
using FreeAIr.Options2.Agent;
using FreeAIr.Options2.Mcp;
using FreeAIr.Options2.Support;
using FreeAIr.Options2.Unsorted;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

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
            var options = await DeserializeAsync();
            return options.AvailableTools;
        }

        public static async Task<McpServers> DeserializeMcpServersAsync()
        {
            var options = await DeserializeAsync();
            return options.AvailableMcpServers;
        }

        public static async Task<UnsortedJson> DeserializeUnsortedAsync()
        {
            var options = await DeserializeAsync();
            return options.Unsorted;
        }

        public static async Task<AgentCollectionJson> DeserializeAgentCollectionAsync()
        {
            var options = await DeserializeAsync();
            return options.AgentCollection;
        }

        public static async Task<SupportCollectionJson> DeserializeSupportCollectionAsync()
        {
            var options = await DeserializeAsync();
            return options.Supports;
        }
        
        public static async Task<FreeAIrOptions> DeserializeAsync()
        {
            try
            {
                var filePath = await ComposeFilePathAsync();
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
                //file does not exists

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
            catch (Exception excp)
            {
                //todo log
            }

            //VS options is not set
            //just create the defaults
            return new FreeAIrOptions();
        }

        public static bool TryDeserializeFromString(
            string optionsJson,
            out FreeAIrOptions? options
            )
        {
            try
            {
                options = DeserializeFromString(optionsJson);
                return true;
            }
            catch
            {
                options = null;
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
            var filePath = await ComposeFilePathAsync();
            if ((!place.HasValue && File.Exists(filePath)) || place == OptionsPlaceEnum.SolutionRelatedFilePath)
            {
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

            var options = await FreeAIrOptions.DeserializeAsync();
            options.AvailableTools = tools;
            await options.SerializeAsync(null);
        }

        public static async Task SaveMcpServersAsync(
            McpServers servers
            )
        {
            if (servers is null)
            {
                throw new ArgumentNullException(nameof(servers));
            }

            var options = await FreeAIrOptions.DeserializeAsync();
            options.AvailableMcpServers = servers;
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

            var options = await FreeAIrOptions.DeserializeAsync();
            options.AgentCollection = agentCollection;
            await options.SerializeAsync(null);
        }

        #endregion

        public static async Task<string> ComposeFilePathAsync()
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
            if (!System.IO.Directory.Exists(folderPath))
            {
                System.IO.Directory.CreateDirectory(folderPath);
            }

            var filePath = System.IO.Path.Combine(
                folderPath,
                $"{solutionName}_options.json"
                );
            return filePath;
        }

        /// <summary>
        /// Where to store options.
        /// </summary>
        public enum OptionsPlaceEnum
        {
            SolutionRelatedFilePath,
            VisualStudioOption
        }
    }
}
