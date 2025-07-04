using FreeAIr.Agents;
using FreeAIr.Embedding;
using FreeAIr.Embedding.Json;
using FreeAIr.Git;
using FreeAIr.Git.Parser;
using FreeAIr.Helper;
using FreeAIr.NLOutline.Tree;
using FreeAIr.NLOutline.Tree.Builder;
using FreeAIr.Shared.Helper;
using FreeAIr.UI.NestedCheckBox;
using FreeAIr.UI.Windows;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    [Export(typeof(BuildNaturalLanguageOutlinesJsonFileToolViewModel))]
    public sealed class BuildNaturalLanguageOutlinesJsonFileToolViewModel : BaseViewModel
    {
        private string _jsonFilePath;
        private ICommand _reloadPageCommand;
        private bool _completeRebuild;
        private ICommand _openJsonFolderCommand;
        private ICommand _updateJsonFileCommand;

        public bool GlobalEnabled
        {
            get
            {
                return SolutionHelper.TryGetSolution(out _);
            }
        }

        public string JsonFilePath
        {
            get => _jsonFilePath;
            private set
            {
                _jsonFilePath = value;
                OnPropertyChanged(nameof(JsonFilePath));
            }
        }

        public ICommand OpenJsonFolderCommand
        {
            get
            {
                if (_openJsonFolderCommand is null)
                {
                    _openJsonFolderCommand = new RelayCommand(
                        a =>
                        {
                            var fi = new FileInfo(_jsonFilePath);
                            var folderPath = fi.Directory.FullName;
                            if (!Directory.Exists(folderPath))
                            {
                                VS.MessageBox.Show("Folder does not exists yet. Save NLO Json file to create the folder.");
                            }

                            Process.Start("explorer.exe", folderPath);
                        }
                        );
                }

                return _openJsonFolderCommand;
            }
        }

        public ICommand ReloadPageCommand
        {
            get
            {
                if (_reloadPageCommand is null)
                {
                    _reloadPageCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            await UpdatePageAsync();
                        });
                }

                return _reloadPageCommand;
            }
        }


        public bool GitRepoExists
        {
            get;
            private set;
        }

        public bool CompleteRebuild
        {
            get => _completeRebuild;
            set
            {
                _completeRebuild = value;

                UpdatePageAsync(false)
                    .FileAndForget(nameof(UpdatePageAsync));
            }
        }

        public ObservableCollection2<CheckableItem> Groups
        {
            get;
        }

        public bool ForceUseNLOAgent
        {
            get;
            set;
        }

        public ObservableCollection2<AgentWrapper> GenerateNLOAgentList
        {
            get;
        }

        public AgentWrapper? SelectedGenerateNLOAgent
        {
            get;
            set;
        }

        public ObservableCollection2<AgentWrapper> GenerateEmbeddingAgentList
        {
            get;
        }

        private readonly SolutionEvents _solutionEvents;

        public AgentWrapper? SelectedGenerateEmbeddingAgent
        {
            get;
            set;
        }



        public string UpdateJsonFileCommandContent
        {
            get
            {
                if (_completeRebuild)
                {
                    return "(Re)write NLO json file completely...";
                }

                return "Update NLO json file...";
            }
        }

        public ICommand UpdateJsonFileCommand
        {
            get
            {
                if (_updateJsonFileCommand is null)
                {
                    _updateJsonFileCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            try
                            {
                                var backgroundTask = new GenerateEmbeddingOutlineFilesBackgroundTask(
                                    SelectedGenerateNLOAgent.Agent,
                                    ForceUseNLOAgent,
                                    SelectedGenerateEmbeddingAgent.Agent,
                                    _completeRebuild,
                                    JsonFilePath,
                                    Groups
                                    );
                                var w = new WaitForTaskWindow(
                                    backgroundTask
                                    );
                                await w.ShowDialogAsync();
                            }
                            catch (Exception excp)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    $"Error: {excp.Message}"
                                    + Environment.NewLine
                                    + excp.StackTrace
                                    );
                            }

                        },
                        a =>
                        {
                            if (string.IsNullOrEmpty(JsonFilePath))
                            {
                                return false;
                            }
                            if (SelectedGenerateNLOAgent is null)
                            {
                                return false;
                            }
                            if (SelectedGenerateEmbeddingAgent is null)
                            {
                                return false;
                            }
                            if (!SolutionHelper.TryGetSolution(out _))
                            {
                                return false;
                            }

                            return true;
                        });
                }

                return _updateJsonFileCommand;
            }
        }

        public BuildNaturalLanguageOutlinesJsonFileToolViewModel(
            )
        {
            Groups = new ObservableCollection2<CheckableItem>();
            GenerateNLOAgentList = new ObservableCollection2<AgentWrapper>();
            GenerateEmbeddingAgentList = new ObservableCollection2<AgentWrapper>();

            _solutionEvents = VS.Events.SolutionEvents;
            _solutionEvents.OnAfterOpenSolution += OnAfterOpenSolution;
            _solutionEvents.OnAfterCloseSolution += OnAfterCloseSolution;
        }

        private async void OnAfterOpenSolution(Solution obj)
        {
            try
            {
                await Task.Delay(1000);

                UpdatePageAsync(true)
                    .FileAndForget(nameof(UpdatePageAsync));
            }
            catch (Exception excp)
            {
                //todo log
            }
        }

        private async void OnAfterCloseSolution()
        {
            try
            {
                await Task.Delay(1000);

                UpdatePageAsync(false)
                    .FileAndForget(nameof(UpdatePageAsync));
            }
            catch (Exception excp)
            {
                //todo log
            }
        }

        public async Task UpdatePageAsync(
            bool fillAgents = true
            )
        {
            if (!SolutionHelper.TryGetSolution(out _))
            {
                ClearAgents();
                ClearGroups();

                OnPropertyChanged();
                return;
            }
            if (!await GitRepositoryProvider.IsGitRepositoryExistsAsync())
            {
                GitRepoExists = false;
                _completeRebuild = true;
            }
            else
            {
                GitRepoExists = true;
            }

            JsonFilePath = await EmbeddingOutlineJsonObject.GenerateFilePathAsync();

            if (fillAgents)
            {
                FillAgents();
            }

            if (_completeRebuild || !await GitRepositoryProvider.IsGitRepositoryExistsAsync())
            {
                FillGroupsFromSolution();
            }
            else
            {
                await FillGroupsFromGitChangesAsync();
            }

            OnPropertyChanged();
        }

        private void FillAgents()
        {
            ClearAgents();

            GenerateNLOAgentList.AddRange(
                InternalPage.Instance.GetAgentCollection().Agents
                    .ConvertAll(a => new AgentWrapper(a))
                    );
            SelectedGenerateNLOAgent = GenerateNLOAgentList.FirstOrDefault();

            GenerateEmbeddingAgentList.AddRange(
                InternalPage.Instance.GetAgentCollection().Agents
                    .ConvertAll(a => new AgentWrapper(a))
                    );
            SelectedGenerateEmbeddingAgent = GenerateEmbeddingAgentList.FirstOrDefault();
        }

        private void ClearAgents()
        {
            GenerateNLOAgentList.Clear();
            SelectedGenerateNLOAgent = null;

            GenerateEmbeddingAgentList.Clear();
            SelectedGenerateEmbeddingAgent = null;
        }

        private async Task FillGroupsFromGitChangesAsync()
        {
            var diff = await GitDiffCreator.BuildGitDiffAsync();

            Dictionary<string, GitDiffFile>? addedFiles = null;
            Dictionary<string, GitDiffFile> updatedFiles = null;
            Dictionary<string, GitDiffFile>? deletedFiles = null;

            if (diff is not null)
            {
                addedFiles = diff.Files
                    .FindAll(f => f.Status == GitDiffFileStatusEnum.Added)
                    .ToDictionary(f => f.NewFullPath, f => f)
                    ;
                updatedFiles = diff.Files
                    .FindAll(f => f.Status == GitDiffFileStatusEnum.Updated)
                    .ToDictionary(f => f.OriginalFullPath, f => f)
                    ;
                deletedFiles = diff.Files
                    .FindAll(f => f.Status == GitDiffFileStatusEnum.Deleted)
                    .ToDictionary(f => f.OriginalFullPath, f => f)
                    ;
            }

            FillGroupsFromSolution(
                addedFiles,
                updatedFiles,
                deletedFiles
                );
        }

        private void FillGroupsFromSolution(
            Dictionary<string, GitDiffFile>? addedFiles = null,
            Dictionary<string, GitDiffFile>? updatedFiles = null,
            Dictionary<string, GitDiffFile>? deletedFiles = null
            )
        {
            ClearGroups();

            if (!SolutionHelper.TryGetSolution(out var solution))
            {
                return;
            }

            var addedUpdatedRoot = solution.ConvertRecursivelyFor<CheckableItem>(
                item =>
                {
                    if (item.Type.NotIn(SolutionItemType.SolutionFolder, SolutionItemType.Solution, SolutionItemType.Project, SolutionItemType.PhysicalFolder, SolutionItemType.PhysicalFile))
                    {
                        return null;
                    }

                    var itemName = item.Name;
                    if (item.Type.In(SolutionItemType.PhysicalFile))
                    {
                        var fi = new FileInfo(item.FullPath);
                        itemName = fi.Name;
                    }
                    if (item.Type.In(SolutionItemType.PhysicalFolder))
                    {
                        var fi = new DirectoryInfo(item.FullPath);
                        itemName = fi.Name;
                    }

                    var fullPathOrName = item.FullPath ?? item.Name;

                    Brush? foreground = null;
                    if (addedFiles is not null && addedFiles.ContainsKey(fullPathOrName))
                    {
                        foreground = Brushes.Green;
                    }
                    if (deletedFiles is not null && deletedFiles.ContainsKey(fullPathOrName))
                    {
                        foreground = Brushes.Red;
                    }

                    Brush? disabledForeground = null;
                    var isChecked = true;
                    if (addedFiles is not null && !addedFiles.ContainsKey(fullPathOrName))
                    {
                        if (updatedFiles is not null && !updatedFiles.ContainsKey(fullPathOrName))
                        {
                            if (deletedFiles is not null && !deletedFiles.ContainsKey(fullPathOrName))
                            {
                                disabledForeground = Brushes.DarkGray;
                                isChecked = false;
                            }
                        }
                    }

                    return new CheckableItem(
                        itemName,
                        string.Empty, //item.FullPath ?? itemName,
                        isChecked,
                        new CheckableItemStyle(
                            foreground,
                            disabledForeground,
                            false
                            ),
                        item
                        );
                },
                (root, child) => root.AddChild(child),
                CancellationToken.None
                );
            Groups.Add(addedUpdatedRoot);
        }

        private void ClearGroups()
        {
            Groups.Clear();
        }
    }

    public sealed class AgentWrapper : BaseViewModel
    {
        public Agent Agent
        {
            get;
        }

        public string AgentName => Agent.Name;

        public string Technical => $"{Agent.Technical.ChosenModel} ({Agent.Technical.Endpoint})";

        public AgentWrapper(
            Agent agent
            )
        {
            if (agent is null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            Agent = agent;
        }

    }


    public sealed class GenerateEmbeddingOutlineFilesBackgroundTask : BackgroundTask
    {
        private readonly Agent _nloAgent;
        private readonly bool _forceUseNLOAgent;
        private readonly Agent _embeddingAgent;
        private readonly bool _completeRebuild;
        private readonly string _jsonFilePath;
        private readonly IReadOnlyList<CheckableItem> _tree;

        public override string TaskDescription => "Please wait for generating files...";

        public string? Result
        {
            get;
            private set;
        }

        public GenerateEmbeddingOutlineFilesBackgroundTask(
            Agent nloAgent,
            bool forceUseNLOAgent,
            Agent embeddingAgent,
            bool completeRebuild,
            string jsonFilePath,
            IReadOnlyList<CheckableItem> tree
            )
        {
            if (nloAgent is null)
            {
                throw new ArgumentNullException(nameof(nloAgent));
            }

            if (embeddingAgent is null)
            {
                throw new ArgumentNullException(nameof(embeddingAgent));
            }

            if (jsonFilePath is null)
            {
                throw new ArgumentNullException(nameof(jsonFilePath));
            }

            if (tree is null)
            {
                throw new ArgumentNullException(nameof(tree));
            }

            _nloAgent = nloAgent;
            _forceUseNLOAgent = forceUseNLOAgent;
            _embeddingAgent = embeddingAgent;
            _completeRebuild = completeRebuild;
            _jsonFilePath = jsonFilePath;
            _tree = tree;
            StartAsyncTask();
        }

        protected override async Task RunWorkingTaskAsync(
            )
        {
            //in case of exception set it null first
            Result = null;

            try
            {
                var outputPanel = await OutlineEmbeddingOutputPanel.CreateOrGetAsync();
                await outputPanel.ActivateAsync();
                await outputPanel.WriteLineAsync();
                await outputPanel.WriteLineAsync(new string('-', 80));
                await outputPanel.WriteLineAsync(DateTime.Now.ToString());

                try
                {
                    await ShowMessageAsync(outputPanel, "Starting...");

                    await ShowMessageAsync(outputPanel, "Start NLO extraction...");

                    HashSet<string>? checkedPaths = null;
                    OutlineNode? existingOutlineRoot = null;
                    if (!_completeRebuild)
                    {
                        (checkedPaths, existingOutlineRoot) = await GetExistingInformationAsync();
                    }

                    var outlineRoot = await TreeBuilder.BuildAsync(
                        parameters: new TreeBuilderParameters(
                            agent: _nloAgent,
                            forceUseNLOAgent: _forceUseNLOAgent,
                            checkedPaths: checkedPaths,
                            oldOutlineRoot: existingOutlineRoot
                            ),
                        cancellationToken: _cancellationTokenSource.Token
                        );

                    await ShowMessageAsync(outputPanel, "Start embedding generation...");

                    var eg = new EmbeddingGenerator(
                        _embeddingAgent
                        );
                    await eg.GenerateEmbeddingsAsync(
                        outlineRoot,
                        _cancellationTokenSource.Token
                        );

                    var jsonObject = new EmbeddingOutlineJsonObject(outlineRoot);
                    await jsonObject.SerializeAsync(
                        _jsonFilePath,
                        CancellationToken.None //cannot be stopped in the middle!
                        );

                    await ShowMessageAsync(outputPanel, "Process SUCESSFULLY completed.");
                    //await outputPane.HideAsync();
                }
                catch (Exception excp)
                {
                    await outputPanel.WriteLineAsync("Error:");
                    await outputPanel.WriteLineAsync(excp.Message);
                    await outputPanel.WriteLineAsync(excp.StackTrace);
                    throw;
                }
            }
            catch (OperationCanceledException)
            {
                //this is ok, nothing to do
            }
            catch (Exception excp)
            {
                await VS.MessageBox.ShowErrorAsync(
                    $"Error: {excp.Message}"
                    + Environment.NewLine
                    + excp.StackTrace
                    );
            }
        }

        private async Task ShowMessageAsync(
            OutputWindowPane panel,
            string message)
        {
            SetNewStatus(message);
            await panel.WriteLineAsync(message);
        }

        private async Task<(HashSet<string>? checkedPaths, OutlineNode? existingOutlineRoot)> GetExistingInformationAsync(
            )
        {
            if (!SolutionHelper.TryGetSolution(out var solution))
            {
                return (null, null);
            }

            var rootPath = solution.FullPath;

            #region local recursive function

            void ProcessItem(
                HashSet<string> checkedPaths,
                CheckableItem item
                )
            {
                if (item.IsChecked.HasValue && !item.IsChecked.Value)
                {
                    return;
                }

                if (item.IsChecked.HasValue && item.IsChecked.Value)
                {
                    var path = item.Tag as string;
                    if (path is null)
                    {
                        if (item.Tag is SolutionItem si)
                        {
                            if (si.Type.In(SolutionItemType.Solution, SolutionItemType.Project, SolutionItemType.PhysicalFile))
                            {
                                path = si.FullPath;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(path))
                    {
                        checkedPaths.Add(
                            path.MakeRelativeAgainst(rootPath)
                            );
                    }
                }

                foreach (var child in item.Children)
                {
                    ProcessItem(checkedPaths, child);
                }
            }

            #endregion

            var checkedPaths = new HashSet<string>();
            foreach (var root in _tree)
            {
                ProcessItem(checkedPaths, root);
            }

            var jsonEmbeddingFilePath = await EmbeddingOutlineJsonObject.GenerateFilePathAsync();
            var existingJson = await EmbeddingOutlineJsonObject.DeserializeAsync(
                jsonEmbeddingFilePath
                );
            await existingJson.LoadEmbeddingsAsync();
            await existingJson.LoadOutlinesAsync();
            await existingJson.LoadOutlineTreeAsync();
            var existingOutlineRoot = OutlineNode.Create(existingJson);

            return (checkedPaths, existingOutlineRoot);
        }
    }

}
