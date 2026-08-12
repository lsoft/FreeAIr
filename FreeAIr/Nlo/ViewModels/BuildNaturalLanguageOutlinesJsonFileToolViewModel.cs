using FreeAIr.Options2.Agent;
using FreeAIr.Embedding;
using FreeAIr.Embedding.Json;
using FreeAIr.Find;
using FreeAIr.Git;
using FreeAIr.Git.Parser;
using FreeAIr.Helper;
using FreeAIr.Interaction;
using FreeAIr.NLOutline.Tree;
using FreeAIr.NLOutline.Tree.Builder;
using FreeAIr.Shared.Helper;
using FreeAIr.UI.NestedCheckBox;
//BackgroundTask, which GenerateEmbeddingOutlineFilesBackgroundTask below derives from, still
//carries the namespace of the window it used to live in; the window itself is gone from here
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
using FreeAIr.Options2;
using FreeAIr.Options2.Support;

namespace FreeAIr.UI.ViewModels
{
    /// <summary>
    /// Backs the "Build natural language outlines JSON file" tool window, the UI that drives
    /// generation of the RAG/NLO embedding index (the outline JSON file the search feature
    /// vectorizes and queries). Lets the user pick which files to include (whole solution or just
    /// the current git changes), which agents to use for outline extraction and embedding, and
    /// kicks off the background task that writes the index.
    /// </summary>
    [Export(typeof(BuildNaturalLanguageOutlinesJsonFileToolViewModel))]
    public sealed class BuildNaturalLanguageOutlinesJsonFileToolViewModel : BaseViewModel
    {
        /// <summary>
        /// Backing flag for <see cref="CompleteRebuild"/>: when set, the next build discards the
        /// existing NLO index and regenerates outlines and embeddings for every file instead of
        /// only the ones that changed since the last run.
        /// </summary>
        private bool _completeRebuild;

        /// <summary>
        /// True while a Visual Studio solution is open; the tool window uses this to enable or
        /// disable the whole "build NLO json file" UI.
        /// </summary>
        public bool GlobalEnabled
        {
            get
            {
                return SolutionHelper.TryGetSolution(out _);
            }
        }

        /// <summary>
        /// Full path of the embedding outline JSON file that this tool reads from and writes to,
        /// as configured on the RAG settings page.
        /// </summary>
        public string JsonFilePath
        {
            get;
            private set
            {
                field = value;
                OnPropertyChanged(nameof(JsonFilePath));
            }
        }

        /// <summary>
        /// Opens Windows Explorer at the folder containing the NLO JSON file, so the user can
        /// inspect or share the generated index file directly.
        /// </summary>
        public ICommand OpenJsonFolderCommand
        {
            get
            {
                if (field is null)
                {
                    field = new RelayCommand(
                        a =>
                        {
                            var fi = new FileInfo(JsonFilePath);
                            var folderPath = fi.Directory.FullName;
                            if (!Directory.Exists(folderPath))
                            {
                                VS.MessageBox.Show(FreeAIr.Resources.Resources.Folder_does_not_exists_yet__Save);
                            }

                            Process.Start("explorer.exe", folderPath);
                        }
                        );
                }

                return field;
            }
        }

        /// <summary>
        /// Re-reads the currently open solution (or the git diff, if incremental mode is active)
        /// and refreshes the file tree and available agents/support actions shown in the tool
        /// window, without touching the saved options.
        /// </summary>
        public ICommand ReloadPageCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(
                        async a =>
                        {
                            await UpdatePageAsync();
                        });
                }

                return field;
            }
        }


        /// <summary>
        /// True when the current solution folder is under git; when false the tool cannot compute
        /// an incremental diff and always falls back to a complete rebuild of the NLO index.
        /// </summary>
        public bool GitRepoExists
        {
            get;
            private set;
        }

        /// <summary>
        /// When checked, the next NLO json build regenerates outlines and embeddings for the whole
        /// solution instead of only the files changed since the last commit; toggling it refreshes
        /// the file tree shown to the user.
        /// </summary>
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

        /// <summary>
        /// The checkable solution/file tree shown in the tool window, letting the user pick which
        /// files are included in the next NLO outline/embedding build.
        /// </summary>
        public ObservableCollection2<CheckableItem> Groups
        {
            get;
        }

        /// <summary>
        /// When set, outline extraction always calls the configured NLO agent even for files where
        /// a cheaper deterministic scanner could otherwise produce the outline.
        /// </summary>
        public bool ForceUseNLOAgent
        {
            get;
            set;
        }

        /// <summary>
        /// Support actions available for the "build natural language outlines" scope, offered to
        /// the user as the action that performs outline extraction.
        /// </summary>
        public ObservableCollection2<SupportActionWrapper> SupportActionList
        {
            get;
        }

        /// <summary>
        /// The support action the user has chosen to drive outline (NLO) extraction for this build.
        /// </summary>
        public SupportActionWrapper? SelectedSupportAction
        {
            get;
            set;
        }


        /// <summary>
        /// Agents available to generate the natural language outlines, offered as choices for
        /// <see cref="SelectedGenerateNLOAgent"/>.
        /// </summary>
        public ObservableCollection2<AgentWrapper> GenerateNLOAgentList
        {
            get;
        }

        /// <summary>
        /// The agent chosen to generate natural language outlines for the files included in this
        /// build.
        /// </summary>
        public AgentWrapper? SelectedGenerateNLOAgent
        {
            get;
            set;
        }

        /// <summary>
        /// Agents available to compute embeddings for the generated outlines, offered as choices
        /// for <see cref="SelectedGenerateEmbeddingAgent"/>.
        /// </summary>
        public ObservableCollection2<AgentWrapper> GenerateEmbeddingAgentList
        {
            get;
        }

        /// <summary>
        /// Subscription to the solution-open/solution-close events that trigger an automatic
        /// refresh of this tool window's file tree.
        /// </summary>
        private readonly SolutionEvents _solutionEvents;

        /// <summary>
        /// The agent chosen to compute embeddings for the outlines produced by
        /// <see cref="SelectedGenerateNLOAgent"/>.
        /// </summary>
        public AgentWrapper? SelectedGenerateEmbeddingAgent
        {
            get;
            set;
        }



        /// <summary>
        /// Caption for <see cref="UpdateJsonFileCommand"/>'s button, switching between "update"
        /// and "rewrite completely" wording depending on <see cref="CompleteRebuild"/>.
        /// </summary>
        public string UpdateJsonFileCommandContent
        {
            get
            {
                if (_completeRebuild)
                {
                    return FreeAIr.Resources.Resources.Re_write_NLO_json_file_completely;
                }

                return FreeAIr.Resources.Resources.Update_NLO_json_file;
            }
        }

        /// <summary>
        /// Runs the full NLO json build: extracts outlines with the chosen support action/agent,
        /// computes embeddings with the chosen embedding agent, calibrates the RAG threshold, and
        /// writes the result to <see cref="JsonFilePath"/>, all inside a modal wait dialog.
        /// </summary>
        public ICommand UpdateJsonFileCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(
                        async a =>
                        {
                            try
                            {
                                var backgroundTask = new GenerateEmbeddingOutlineFilesBackgroundTask(
                                    SelectedSupportAction.SupportAction,
                                    SelectedGenerateNLOAgent.Agent,
                                    ForceUseNLOAgent,
                                    SelectedGenerateEmbeddingAgent.Agent,
                                    _completeRebuild,
                                    JsonFilePath,
                                    Groups
                                    );
                                var componentModel = await MefHelper.GetComponentModelAsync();
                                await componentModel.GetService<IBackgroundTaskShower>().ShowAsync(
                                    backgroundTask
                                    );
                            }
                            catch (Exception excp)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    Resources.Resources.Error +  $": {excp.Message}"
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
                            if (SelectedSupportAction is null)
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

                return field;
            }
        }

        /// <summary>
        /// Creates the view model, wiring up the solution open/close events that keep the tool
        /// window's file tree and agent lists in sync with the currently loaded solution.
        /// </summary>
        public BuildNaturalLanguageOutlinesJsonFileToolViewModel(
            )
        {
            Groups = new ObservableCollection2<CheckableItem>();
            SupportActionList = new ObservableCollection2<SupportActionWrapper>();
            GenerateNLOAgentList = new ObservableCollection2<AgentWrapper>();
            GenerateEmbeddingAgentList = new ObservableCollection2<AgentWrapper>();

            _solutionEvents = VS.Events.SolutionEvents;
            _solutionEvents.OnAfterOpenSolution += OnAfterOpenSolution;
            _solutionEvents.OnAfterCloseSolution += OnAfterCloseSolution;
        }

        /// <summary>
        /// Handles Visual Studio's "solution opened" event: waits a moment for the solution to
        /// settle, then does a full refresh of the tool window (agents, support actions and file
        /// tree) as if the user had reopened it.
        /// </summary>
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
                excp.ActivityLogException();
            }
        }

        /// <summary>
        /// Handles Visual Studio's "solution closed" event by clearing the tool window's file tree
        /// and agent lists, since there is no longer a solution to build an NLO index for.
        /// </summary>
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
                excp.ActivityLogException();
            }
        }

        /// <summary>
        /// Refreshes the whole tool window: detects whether a git repository backs the solution,
        /// resolves the NLO json file path, optionally reloads the support action and agent lists
        /// from options, and rebuilds the file tree either from the full solution or from the
        /// current git diff.
        /// </summary>
        public async Task UpdatePageAsync(
            bool reloadFromOptions = true
            )
        {
            if (!SolutionHelper.TryGetSolution(out _))
            {
                ClearSupportActions();
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

            JsonFilePath = await FreeAIrOptions.ComposeEmbeddingsFilePathAsync();

            if (reloadFromOptions)
            {
                await FillSupportActionsAsync();
                await FillAgentsAsync();
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

        /// <summary>
        /// Loads the support actions scoped to <see cref="SupportScopeEnum.BuildNaturalLanguageOutlines"/>
        /// into <see cref="SupportActionList"/> and selects the first one as the default.
        /// </summary>
        private async Task FillSupportActionsAsync()
        {
            ClearSupportActions();

            var actions = await FreeAIrOptions.DeserializeSupportActionsAsync(
                a => a.Scopes.Contains(SupportScopeEnum.BuildNaturalLanguageOutlines)
                );

            SupportActionList.AddRange(
                actions.ConvertAll(a => new SupportActionWrapper(a))
                );
            SelectedSupportAction = SupportActionList.FirstOrDefault();
        }

        /// <summary>
        /// Empties <see cref="SupportActionList"/> and clears the current selection.
        /// </summary>
        private void ClearSupportActions()
        {
            SupportActionList.Clear();
            SelectedSupportAction = null;
        }

        /// <summary>
        /// Loads the configured agent collection into both <see cref="GenerateNLOAgentList"/> and
        /// <see cref="GenerateEmbeddingAgentList"/> (the same agents can be used for either role)
        /// and selects the first agent as the default for each.
        /// </summary>
        private async Task FillAgentsAsync()
        {
            ClearAgents();

            var agentCollection = await FreeAIrOptions.DeserializeAgentCollectionAsync();

            GenerateNLOAgentList.AddRange(
                agentCollection.Agents
                    .ConvertAll(a => new AgentWrapper(a))
                    );
            SelectedGenerateNLOAgent = GenerateNLOAgentList.FirstOrDefault();

            GenerateEmbeddingAgentList.AddRange(
                agentCollection.Agents
                    .ConvertAll(a => new AgentWrapper(a))
                    );
            SelectedGenerateEmbeddingAgent = GenerateEmbeddingAgentList.FirstOrDefault();
        }

        /// <summary>
        /// Empties both the NLO-generation and embedding-generation agent lists and clears their
        /// current selections.
        /// </summary>
        private void ClearAgents()
        {
            GenerateNLOAgentList.Clear();
            SelectedGenerateNLOAgent = null;

            GenerateEmbeddingAgentList.Clear();
            SelectedGenerateEmbeddingAgent = null;
        }

        /// <summary>
        /// Builds the file tree shown to the user restricted to what changed against git (added,
        /// updated and deleted files), so an incremental NLO build only touches those files. Falls
        /// back to the full solution tree when there is no diff to show.
        /// </summary>
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

        /// <summary>
        /// Converts the current Visual Studio solution into the checkable tree shown in
        /// <see cref="Groups"/>, color-coding added/deleted files and pre-unchecking files that
        /// are unrelated to the git diff (when one is supplied), so the user can review exactly
        /// what will go into the next NLO json build.
        /// </summary>
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

        /// <summary>
        /// Empties the checkable file tree shown in the tool window.
        /// </summary>
        private void ClearGroups()
        {
            Groups.Clear();
        }
    }

    /// <summary>
    /// Wraps a <see cref="SupportActionJson"/> for display in the "generate outlines" support
    /// action picker of the NLO json build tool window.
    /// </summary>
    public sealed class SupportActionWrapper : BaseViewModel
    {
        /// <summary>
        /// The wrapped support action definition used to drive NLO outline extraction.
        /// </summary>
        public SupportActionJson SupportAction
        {
            get;
        }

        /// <summary>
        /// Display name of the wrapped support action, shown in the picker's list.
        /// </summary>
        public string SupportActionName => SupportAction.Name;

        /// <summary>
        /// Wraps the given support action for binding in the NLO build tool window.
        /// </summary>
        public SupportActionWrapper(
            SupportActionJson supportAction
            )
        {
            if (supportAction is null)
            {
                throw new ArgumentNullException(nameof(supportAction));
            }

            SupportAction = supportAction;
        }

    }

    /// <summary>
    /// Wraps an <see cref="AgentJson"/> for display in the NLO-agent and embedding-agent pickers
    /// of the NLO json build tool window.
    /// </summary>
    public sealed class AgentWrapper : BaseViewModel
    {
        /// <summary>
        /// The wrapped agent configuration used for outline extraction or embedding generation.
        /// </summary>
        public AgentJson Agent
        {
            get;
        }

        /// <summary>
        /// Display name of the wrapped agent, shown in the picker's list.
        /// </summary>
        public string AgentName => Agent.Name;

        /// <summary>
        /// The agent's chosen model and endpoint, formatted for display next to its name so the
        /// user can tell which server/model a given agent points at.
        /// </summary>
        public string Technical => $"{Agent.Technical.ChosenModel} ({Agent.Technical.Endpoint})";

        /// <summary>
        /// Wraps the given agent for binding in the NLO build tool window.
        /// </summary>
        public AgentWrapper(
            AgentJson agent
            )
        {
            if (agent is null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            Agent = agent;
        }

    }


    /// <summary>
    /// Background task that performs the actual NLO json build: extracts outlines from the
    /// selected files, generates embeddings for them, calibrates the RAG similarity threshold and
    /// serializes the result to the embedding outline JSON file, reporting progress to the
    /// output panel and a wait dialog while it runs.
    /// </summary>
    public sealed class GenerateEmbeddingOutlineFilesBackgroundTask : BackgroundTask
    {
        /// <summary>
        /// Support action used to extract natural language outlines from the selected files.
        /// </summary>
        private readonly SupportActionJson _nloAction;
        /// <summary>
        /// Agent used to run the outline extraction support action.
        /// </summary>
        private readonly AgentJson _nloAgent;
        /// <summary>
        /// When true, outline extraction always goes through the NLO agent instead of a
        /// deterministic scanner where one would otherwise apply.
        /// </summary>
        private readonly bool _forceUseNLOAgent;
        /// <summary>
        /// Agent used to compute the embedding vectors for the extracted outlines.
        /// </summary>
        private readonly AgentJson _embeddingAgent;
        /// <summary>
        /// When true, the existing NLO index is discarded and every selected file is re-outlined
        /// and re-embedded, instead of reusing what an incremental build could keep.
        /// </summary>
        private readonly bool _completeRebuild;
        /// <summary>
        /// Destination path of the embedding outline JSON file this task writes.
        /// </summary>
        private readonly string _jsonFilePath;
        /// <summary>
        /// The checkable solution/file tree describing which files the user selected to include
        /// in this build.
        /// </summary>
        private readonly IReadOnlyList<CheckableItem> _tree;

        /// <summary>
        /// Status text shown on the wait dialog while this task runs.
        /// </summary>
        public override string TaskDescription => FreeAIr.Resources.Resources.Please_wait_for_generating_files;

        /// <summary>
        /// Reserved for a result summary of the build; currently unused beyond being reset at the
        /// start of each run.
        /// </summary>
        public string? Result
        {
            get;
            private set;
        }

        /// <summary>
        /// Creates and immediately starts the background task that builds the NLO json file from
        /// the given agents, support action and selected file tree.
        /// </summary>
        public GenerateEmbeddingOutlineFilesBackgroundTask(
            SupportActionJson nloAction,
            AgentJson nloAgent,
            bool forceUseNLOAgent,
            AgentJson embeddingAgent,
            bool completeRebuild,
            string jsonFilePath,
            IReadOnlyList<CheckableItem> tree
            )
        {
            if (nloAction is null)
            {
                throw new ArgumentNullException(nameof(nloAction));
            }

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

            _nloAction = nloAction;
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
                    await ShowMessageAsync(outputPanel, FreeAIr.Resources.Resources.Starting);

                    await ShowMessageAsync(outputPanel, FreeAIr.Resources.Resources.Start_NLO_extraction);

                    HashSet<string>? checkedPaths = null;
                    OutlineNode? existingOutlineRoot = null;
                    if (!_completeRebuild)
                    {
                        (checkedPaths, existingOutlineRoot) = await GetExistingInformationAsync();
                    }

                    var outlineRoot = await TreeBuilder.BuildAsync(
                        parameters: new TreeBuilderParameters(
                            action: _nloAction,
                            agent: _nloAgent,
                            forceUseNLOAgent: _forceUseNLOAgent,
                            checkedPaths: checkedPaths,
                            oldOutlineRoot: existingOutlineRoot
                            ),
                        cancellationToken: _cancellationTokenSource.Token,
                        onProgress: (processed, total) =>
                        {
                            SetNewStatus(
                                $"{FreeAIr.Resources.Resources.Start_NLO_extraction} ({processed}/{total})"
                                );
                        }
                        );
                    if (outlineRoot is null)
                    {
                        return;
                    }

                    var nodesToEmbedCount = OutlineEmbedder.SelectNodesToEmbed(outlineRoot).Count;
                    await ShowMessageAsync(
                        outputPanel,
                        $"{FreeAIr.Resources.Resources.Start_embedding_generation} (0/{nodesToEmbedCount})"
                        );

                    //one vectorizer for the whole build: the calibration below asks it which model
                    //the server said it was, and that is only known after the first request
                    var vectorizer = AgentEmbedding.CreateVectorizer(_embeddingAgent);

                    var eg = new OutlineEmbedder(
                        vectorizer
                        );
                    await eg.GenerateEmbeddingsAsync(
                        outlineRoot,
                        _cancellationTokenSource.Token
                        );

                    await ShowMessageAsync(
                        outputPanel,
                        $"{FreeAIr.Resources.Resources.Start_embedding_generation} ({nodesToEmbedCount}/{nodesToEmbedCount})"
                        );

                    var jsonObject = new EmbeddingOutlineJsonObject(
                        outlineRoot,
                        _embeddingAgent.Name,
                        _embeddingAgent.Technical.ChosenModel,
                        _embeddingAgent.Technical.Endpoint
                        );

                    await CalibrateAsync(
                        jsonObject,
                        vectorizer,
                        outputPanel,
                        _cancellationTokenSource.Token
                        );

                    await jsonObject.SerializeAsync(
                        _jsonFilePath,
                        CancellationToken.None //cannot be stopped in the middle!
                        );

                    //the cache keys itself on the write time of the files, so it would notice on
                    //its own; this is about releasing the megabytes of the previous index now
                    //rather than at the next search
                    (await GetIndexContainerAsync()).Invalidate();

                    await ShowMessageAsync(outputPanel, FreeAIr.Resources.Resources.Process_SUCESSFULLY_completed);
                    //await outputPane.HideAsync();
                }
                catch (Exception excp)
                {
                    await outputPanel.WriteLineAsync(Resources.Resources.Error + ":");
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

        /// <summary>
        /// Measures where this model's similarity lies on this solution and stores the numbers in
        /// the index, so that the search has a threshold which means the same thing whichever
        /// embedding model the user has chosen. Also writes down the fingerprint of the model, so
        /// that a search with another one is refused instead of silently returning noise.
        ///
        /// A failure here is reported and swallowed: an index without calibration is a working
        /// index — the search simply applies no threshold to it — and losing an hour of embedding
        /// over a hiccup of the server at the very end of it would not be a fair trade.
        /// </summary>
        private async Task CalibrateAsync(
            EmbeddingOutlineJsonObject jsonObject,
            IEmbeddingVectorizer vectorizer,
            OutputWindowPane outputPanel,
            CancellationToken cancellationToken
            )
        {
            await ShowMessageAsync(outputPanel, FreeAIr.Resources.Resources.RAG__calibrating_the_index);

            var rag = await FreeAIrOptions.DeserializeRagAsync();

            try
            {
                await RagCalibrator.ApplyToAsync(
                    jsonObject,
                    vectorizer,
                    AgentEmbedding.CreateCalibrationProbes(rag),
                    cancellationToken
                    );
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception excp)
            {
                await outputPanel.WriteLineAsync(
                    string.Format(
                        FreeAIr.Resources.Resources.RAG__calibration_failed__0_,
                        excp.Message
                        )
                    );
                return;
            }

            var calibration = jsonObject.Calibration?.ToCalibration();
            if (calibration is null)
            {
                return;
            }

            await outputPanel.WriteLineAsync(
                string.Format(
                    FreeAIr.Resources.Resources.RAG__calibrated__noise__0___threshold__1_,
                    calibration.NoiseCeiling.ToString("F3"),
                    calibration.ComputeThreshold(rag.Sensitivity).ToString("F3")
                    )
                );

            if (calibration.RelevantMissCount > 0)
            {
                await outputPanel.WriteLineAsync(
                    string.Format(
                        FreeAIr.Resources.Resources.RAG__calibration__0__of__1__probes_missed,
                        calibration.RelevantMissCount,
                        calibration.RelevantProbeCount
                        )
                    );
            }

            if (!calibration.ModelSeparates)
            {
                //the one outcome no setting can rescue: the model scores nonsense as high as the
                //answers the user has pointed at
                await outputPanel.WriteLineAsync(
                    string.Format(
                        FreeAIr.Resources.Resources.RAG__calibration_model_does_not_separate,
                        calibration.RelevantFloor.ToString("F3"),
                        calibration.NoiseCeiling.ToString("F3")
                        )
                    );
            }
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

            //with the vectors: the whole point of an incremental rebuild is to reuse the outlines
            //of the untouched files together with the embeddings already paid for
            var existingOutlineRoot = await (await GetIndexContainerAsync()).GetOutlineTreeAsync(
                true,
                cancellationToken: _cancellationTokenSource.Token
                );

            //the container reads the index on a background thread and leaves the caller there,
            //while the rest of the rebuild walks the solution
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_cancellationTokenSource.Token);

            return (checkedPaths, existingOutlineRoot);
        }

        private static async Task<EmbeddingIndexContainer> GetIndexContainerAsync(
            )
        {
            var componentModel = await MefHelper.GetComponentModelAsync();
            return componentModel.GetService<EmbeddingIndexContainer>();
        }
    }

}
