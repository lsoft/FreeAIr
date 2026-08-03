using FreeAIr.Embedding;
using FreeAIr.Find;
using FreeAIr.Helper;
using FreeAIr.Options2;
using FreeAIr.Options2.Support;
using FreeAIr.Shared.Helper;
using FreeAIr.UI.Embedillo.Answer.Parser;
using FreeAIr.UI.ToolWindows;
using FuzzySharp;
using FuzzySharp.PreProcess;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WpfHelpers;
using FreeAIr.Chat;
using FreeAIr.Chat.Context;
using FreeAIr.Chat.Context.Item;

namespace FreeAIr.UI.ViewModels
{
    [Export(typeof(NaturalLanguageResultsViewModel))]
    public sealed class NaturalLanguageResultsViewModel : BaseViewModel
    {
        private FreeAIr.Chat.Chat? _chat;
        private string _status = Resources.Resources.Idle;

        private double _progressValue;
        private double _progressMaximum;
        private bool _isProgressIndeterminate;
        private Visibility _progressVisibility = Visibility.Collapsed;

        private string _agentsDescription = string.Empty;

        private Visibility _ragPanelVisibility = Visibility.Collapsed;
        private string _ragIndexDescription = string.Empty;
        private string _candidatesHeader = string.Empty;
        private Visibility _uncoveredVisibility = Visibility.Collapsed;
        private string _uncoveredText = string.Empty;
        private List<string> _uncoveredFiles = new();

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private Task? _processingTask;

        public string Status
        {
            get => _status;
            private set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public double ProgressValue
        {
            get => _progressValue;
            private set
            {
                _progressValue = value;
                OnPropertyChanged(nameof(ProgressValue));
            }
        }

        public double ProgressMaximum
        {
            get => _progressMaximum;
            private set
            {
                _progressMaximum = value;
                OnPropertyChanged(nameof(ProgressMaximum));
            }
        }

        /// <summary>
        /// True for the steps whose length cannot be known in advance — everything which waits for
        /// a server, basically.
        /// </summary>
        public bool IsProgressIndeterminate
        {
            get => _isProgressIndeterminate;
            private set
            {
                _isProgressIndeterminate = value;
                OnPropertyChanged(nameof(IsProgressIndeterminate));
            }
        }

        public Visibility ProgressVisibility
        {
            get => _progressVisibility;
            private set
            {
                _progressVisibility = value;
                OnPropertyChanged(nameof(ProgressVisibility));
            }
        }

        /// <summary>
        /// Who has been asked, and — for a RAG search — who has turned the query into a vector.
        /// Those are two different agents chosen in two different ways, and the answers of the
        /// search cannot be judged without knowing which model produced them.
        /// </summary>
        public string AgentsDescription
        {
            get => _agentsDescription;
            private set
            {
                _agentsDescription = value;
                OnPropertyChanged(nameof(AgentsDescription));
                OnPropertyChanged(nameof(AgentsVisibility));
            }
        }

        public Visibility AgentsVisibility =>
            string.IsNullOrEmpty(_agentsDescription)
                ? Visibility.Collapsed
                : Visibility.Visible
                ;

        /// <summary>
        /// The shortlist header is only shown for a RAG search — an ordinary one reads every file
        /// of the scope and has nothing to explain.
        /// </summary>
        public Visibility RagPanelVisibility
        {
            get => _ragPanelVisibility;
            private set
            {
                _ragPanelVisibility = value;
                OnPropertyChanged(nameof(RagPanelVisibility));
            }
        }

        public string RagIndexDescription
        {
            get => _ragIndexDescription;
            private set
            {
                _ragIndexDescription = value;
                OnPropertyChanged(nameof(RagIndexDescription));
            }
        }

        public string CandidatesHeader
        {
            get => _candidatesHeader;
            private set
            {
                _candidatesHeader = value;
                OnPropertyChanged(nameof(CandidatesHeader));
            }
        }

        public Visibility UncoveredVisibility
        {
            get => _uncoveredVisibility;
            private set
            {
                _uncoveredVisibility = value;
                OnPropertyChanged(nameof(UncoveredVisibility));
            }
        }

        public string UncoveredText
        {
            get => _uncoveredText;
            private set
            {
                _uncoveredText = value;
                OnPropertyChanged(nameof(UncoveredText));
            }
        }

        public ObservableCollection2<FoundResultItem> FoundItems
        {
            get;
        }

        /// <summary>
        /// The files the shortlist has picked, best first, shown before the first answer of the LLM
        /// arrives: this is the only moment where the user can tell that the search is looking in
        /// the wrong place, and stop it.
        /// </summary>
        public ObservableCollection2<RagCandidate> Candidates
        {
            get;
        }

        public ICommand GotoCommand
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
                                var foundItem = a as FoundResultItem;
                                if (foundItem is null)
                                {
                                    return;
                                }

                                var documentView = await VS.Documents.OpenAsync(foundItem.FilePath);
                                if (documentView is null)
                                {
                                    return;
                                }

                                var textView = documentView.TextView;

                                var snapshot = textView.TextSnapshot;

                                FindSelectionFromLLMData(
                                    snapshot,
                                    foundItem,
                                    out var startLineIndex,
                                    out var startColumnIndex,
                                    out var startOffset,
                                    out var endOffset,
                                    out var endLineIndex,
                                    out var endColumnIndex
                                    );

                                var offsetText = snapshot.GetText(startOffset, endOffset - startOffset);
                                var fuzzyRatio = Fuzz.PartialRatio(offsetText, foundItem.FoundText, PreprocessMode.Full);
                                if(fuzzyRatio < 70)
                                {
                                    //нейросеть напутала с адресом
                                    var result = FindSelectionFromDocumentBody(
                                        snapshot,
                                        foundItem,
                                        ref startLineIndex,
                                        ref startColumnIndex,
                                        ref endLineIndex,
                                        ref endColumnIndex
                                        );
                                    if (!result)
                                    {
                                        await VS.MessageBox.ShowErrorAsync(
                                            Resources.Resources.Error,
                                            FreeAIr.Resources.Resources.Cannot_determine_the_position_of
                                            );

                                        startLineIndex = 0;
                                        startColumnIndex = 0;
                                        endLineIndex = 0;
                                        endColumnIndex = 0;
                                    }
                                }

                                var docViewType = default(Guid);
                                if (textView.ToIVsTextView().GetBuffer(out var buffer) != VSConstants.S_OK)
                                {
                                    return;
                                }
                                var textManager = Package.GetGlobalService(typeof(VsTextManagerClass)) as IVsTextManager;
                                textManager.NavigateToLineAndColumn(
                                    buffer,
                                    ref docViewType,
                                    startLineIndex,
                                    startColumnIndex,
                                    endLineIndex,
                                    endColumnIndex
                                    );

                            }
                            catch(Exception excp)
                            {
                                excp.ActivityLogException();
                            }
                        }
                        );
                }

                return field;
            }
        }

        public ICommand CancelChatCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(
                        async a =>
                        {
                            var processingTask = Interlocked.Exchange(ref _processingTask, null);
                            if (processingTask is null)
                            {
                                return;
                            }
                            if (processingTask.IsCompleted || processingTask.IsCanceled || processingTask.IsFaulted)
                            {
                                return;
                            }

                            _cancellationTokenSource.Cancel();

                            try
                            {
                                await processingTask;
                            }
                            catch (OperationCanceledException)
                            {
                                //that is what the button is for. The search reports the cancellation
                                //in its own status line; rethrowing here would only take down the
                                //command with an unobserved exception.
                            }

                            OnPropertyChanged();
                        },
                        a =>
                        {
                            var processingTask = _processingTask;
                            if (processingTask is null)
                            {
                                return false;
                            }

                            if (processingTask.IsCompleted || processingTask.IsCanceled || processingTask.IsFaulted)
                            {
                                return false;
                            }

                            return true;
                        }
                        );
                }

                return field;
            }
        }

        public ICommand ShowUncoveredFilesCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(
                        async a =>
                        {
                            await VS.MessageBox.ShowWarningAsync(
                                Resources.Resources.RAG__these_files_are_not_covered,
                                string.Join(Environment.NewLine, _uncoveredFiles)
                                );
                        },
                        a => _uncoveredFiles.Count > 0
                        );
                }

                return field;
            }
        }

        public NaturalLanguageResultsViewModel()
        {
            FoundItems = new();
            Candidates = new();
        }

        /// <summary>
        /// Nobody awaits this - it is started and forgotten by <see cref="ShowPanelAsync"/> - so an
        /// exception escaping it would end up in the activity log and nowhere else, leaving a window
        /// which stays on `Idle` for ever. Whatever goes wrong goes into the status line instead.
        /// </summary>
        public async Task SetNewChatAsync(
            NaturalLanguageSearchParameters parameters
            )
        {
            try
            {
                await SetNewChatCoreAsync(parameters);
            }
            catch (Exception excp)
            {
                SearchTrace.Fail("The search could not be started", excp);

                Status = Resources.Resources.Error + $": {excp.Message}";
            }
        }

        private async Task SetNewChatCoreAsync(
            NaturalLanguageSearchParameters parameters
            )
        {
            if (parameters is null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            //the panel is a singleton, so a second search arrives while the first one may still be
            //running. It has to be stopped and waited for before its token source is disposed:
            //otherwise it goes on using a disposed one and dies of ObjectDisposedException in the
            //middle, which is neither reported anywhere nor recoverable.
            _cancellationTokenSource?.Cancel();

            var oldChat = Interlocked.Exchange(ref _chat, null);
            if (oldChat is not null)
            {
                await oldChat.StopAsync();
            }

            var oldProcessingTask = Interlocked.Exchange(ref _processingTask, null);
            if (oldProcessingTask is not null)
            {
                try
                {
                    await oldProcessingTask;
                }
                catch (OperationCanceledException)
                {
                    //we have just cancelled it ourselves
                }
            }

            if (oldChat is not null)
            {
                await oldChat.DisposeAsync();
            }

            _cancellationTokenSource?.Dispose();

            var filesTypeFilters = new FileTypesFilter(
                parameters.FileTypesFilterText
                    .Split(';')
                    .ConvertAll(f => new FileTypeFilter(f))
                );

            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
            var chatContainer = componentModel.GetService<ChatContainer>();

            var chat = await chatContainer.StartChatAsync(
                new ChatDescription(
                    null
                    ),
                null,
                await ChatOptions.NoToolAutoProcessedJsonResponseAsync(parameters.ChosenAgent)
                );
            if (chat is null)
            {
                //the agent verification has already told the user what is wrong with the agent;
                //this is about the search, which is over before it has begun
                SearchTrace.Stop(Resources.Resources.Search__the_chat_has_not_started);

                Status = Resources.Resources.Search__the_chat_has_not_started;
                return;
            }

            SearchTrace.Step($"Chat {chat.Id} created with the agent '{parameters.ChosenAgent.Name}'.");

            _chat = chat;
            _cancellationTokenSource = new CancellationTokenSource();

            //the chat is captured rather than read from the field: by the time a cancellation of
            //this search runs, the field may already point at the chat of the next one
            _cancellationTokenSource.Token.Register(
                () =>
                {
                    chat.StopAsync()
                        .FileAndForget(nameof(FreeAIr.Chat.Chat.StopAsync));
                });

            _processingTask = ProcessSolutionDocumentsAsync(
                parameters,
                filesTypeFilters
                );
        }

        private async Task ProcessSolutionDocumentsAsync(
            NaturalLanguageSearchParameters parameters,
            FileTypesFilter filesTypeFilters
            )
        {
            if (parameters is null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (filesTypeFilters is null)
            {
                throw new ArgumentNullException(nameof(filesTypeFilters));
            }

            var cancellationToken = _cancellationTokenSource.Token;

            FoundItems.Clear();

            AgentsDescription = DescribeAgents(parameters);

            //the panel is reused by every search, and a shortlist left over from the previous one
            //would be read as belonging to this query
            Candidates.Clear();
            _uncoveredFiles = new List<string>();
            RagPanelVisibility = Visibility.Collapsed;
            UncoveredVisibility = Visibility.Collapsed;
            RagIndexDescription = string.Empty;
            CandidatesHeader = string.Empty;

            List<FoundResultItem> foundItems = new();

            try
            {
                var root = await DetermineRootAsync(parameters.ChosenScope);
                if (root is null)
                {
                    SearchTrace.Stop(Resources.Resources.Search__there_is_nothing_to_search_in);

                    Status = Resources.Resources.Search__there_is_nothing_to_search_in;
                    return;
                }

                //walking the solution tree is the first cancellable step, and it is inside the try
                //for exactly that reason: cancelling here has to end up in the status line like
                //cancelling anywhere else, not as an exception nobody observes
                var foundRootItems = await root.ProcessDownRecursivelyForAsync(
                    item =>
                    {
                        if (item.Type != SolutionItemType.PhysicalFile)
                        {
                            return false;
                        }

                        if (FileTypeHelper.GetFileType(item.FullPath) != FileTypeEnum.Text)
                        {
                            return false;
                        }

                        if (!filesTypeFilters.Match(item.FullPath))
                        {
                            return false;
                        }

                        return true;
                    },
                    false,
                    cancellationToken
                    );

                SearchTrace.Step(
                    $"{foundRootItems.Count} file(s) of the scope match the mask '{parameters.FileTypesFilterText}'."
                    );

                if (parameters.UseRAG)
                {
                    var narrowed = await ApplyRagAsync(
                        parameters,
                        foundRootItems,
                        cancellationToken
                        );
                    if (narrowed is null)
                    {
                        //the reason is already on the screen
                        return;
                    }

                    foundRootItems = narrowed;
                }

                var processedItemCount = 0;

                //todo неправильно считается размер порции: не учитываются related context items
                //которые будут добавлены в контекст чата по референсам для C#
                //надо добавлять по одному файлу, добавлять все референс-файлы и оценивать размер
                //если размер стал слишком большим - откатывать последний файл и его референс файлы
                //и использовать их на следующей итерации
                foreach (var portion in foundRootItems.SplitByItemsSize(parameters.ChosenAgent.Technical.ContextSize))
                {
                    Status = string.Format(
                        Resources.Resources.In_progress___0___1,
                        processedItemCount,
                        foundRootItems.Count
                        );
                    ReportProgress(processedItemCount, foundRootItems.Count);

                    cancellationToken.ThrowIfCancellationRequested();

                    var contextItems = new List<IChatContextItem>();

                    foreach (var solutionItem in portion)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var contextItem = new SolutionItemChatContextItem(
                            SelectedIdentifier.Create(
                                solutionItem.SolutionItem.FullPath,
                                null
                                ),
                            false,
                            AddLineNumbersMode.RequiredAllInScope
                            );
                        contextItems.Add(contextItem);
                    }
                    _chat.ChatContext.AddItems(contextItems);

                    var supportContext = await SupportContext.WithNaturalLanguageSearchQueryAsync(
                        parameters.SearchText
                        );

                    var promptText = supportContext.ApplyVariablesToPrompt(
                        parameters.ChosenAction.Prompt
                        );

                    _chat.AddPrompt(
                        UserPrompt.CreateTextBasedPrompt(
                            promptText
                            )
                        );

                    SearchTrace.Step(
                        $"Asking about {portion.Count} file(s), the prompt is {promptText.Length} char(s) long."
                        );

                    var cleanAnswer = await _chat.WaitForPromptCleanAnswerAsync(
                        Environment.NewLine
                        );

                    SearchTrace.Step(
                        string.IsNullOrEmpty(cleanAnswer)
                            ? "The agent has answered nothing."
                            : $"The agent has answered {cleanAnswer.Length} char(s)."
                        );

                    if (!string.IsNullOrEmpty(cleanAnswer))
                    {
                        FillFoundItemsByLLMJson(
                            cleanAnswer,
                            ref foundItems
                            );

                        foundItems = foundItems.OrderByDescending(i => i.ConfidenceLevel).ToList();

                        FoundItems.Clear();
                        FoundItems.AddRange(foundItems);

                        OnPropertyChanged();
                    }

                    _chat.ArchiveAllPrompts();
                    _chat.ChatContext.RemoveItems(contextItems);

                    processedItemCount += portion.Count;
                }

                Status = string.Format(
                    Resources.Resources.Found__0__items_,
                    FoundItems.Count
                    );

                SearchTrace.Step($"Done, {FoundItems.Count} item(s) found.");
            }
            catch (OperationCanceledException)
            {
                //this is ok
                Status = Resources.Resources.Cancelled;

                SearchTrace.Step("Cancelled.");
            }
            catch (Exception excp)
            {
                SearchTrace.Fail("The search has failed", excp);

                Status = Resources.Resources.Error + $": {excp.Message}";
            }
            finally
            {
                HideProgress();
            }

            OnPropertyChanged();
        }

        /// <summary>
        /// One line per agent the search uses. Without RAG there is one, and it is the agent the
        /// user has just picked; with RAG there is a second one, which the user has usually not
        /// picked at all — it comes from the index, which remembers the agent that built it.
        /// </summary>
        private static string DescribeAgents(
            NaturalLanguageSearchParameters parameters
            )
        {
            var description = string.Format(
                Resources.Resources.Search__agent__0___1___2_,
                parameters.ChosenAgent.Name,
                parameters.ChosenAgent.Technical.Endpoint,
                parameters.ChosenAgent.Technical.ChosenModel
                );

            if (parameters.UseRAG && parameters.EmbeddingAgent is not null)
            {
                description +=
                    Environment.NewLine
                    + string.Format(
                        Resources.Resources.Search__embedding_agent__0___1_,
                        parameters.EmbeddingAgent.Name,
                        parameters.EmbeddingAgent.Technical.Endpoint
                        );
            }

            return description;
        }

        /// <summary>
        /// Replaces the whole scope of the search with the handful of files whose outlines are the
        /// closest to the query. Returns null when the search has to stop — either there is nothing
        /// to search in, or the index cannot answer at all; in both cases the reason has already
        /// been shown to the user.
        /// </summary>
        private async Task<List<SolutionHelper.FoundSolutionItem>?> ApplyRagAsync(
            NaturalLanguageSearchParameters parameters,
            List<SolutionHelper.FoundSolutionItem> foundRootItems,
            CancellationToken cancellationToken
            )
        {
            var solution = await VS.Solutions.GetCurrentSolutionAsync();
            if (solution is null)
            {
                SearchTrace.Stop(Resources.Resources.Search__there_is_nothing_to_search_in);

                Status = Resources.Resources.Search__there_is_nothing_to_search_in;
                return null;
            }

            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
            var indexContainer = componentModel.GetService<EmbeddingIndexContainer>();

            Status = Resources.Resources.RAG__preparing_the_index;
            ShowProgress(true);

            var index = await indexContainer.GetAsync(
                new IndexLoadProgress(this),
                cancellationToken
                );

            //GetAsync leaves us on a background thread, and everything below touches the collections
            //bound to the panel
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (index is null)
            {
                Status = Resources.Resources.RAG__there_is_no_index;

                SearchTrace.Stop(Resources.Resources.RAG__there_is_no_index);

                await VS.MessageBox.ShowErrorAsync(
                    Resources.Resources.Error,
                    Resources.Resources.RAG__there_is_no_index
                    );

                return null;
            }

            RagPanelVisibility = Visibility.Visible;
            RagIndexDescription = string.Format(
                Resources.Resources.RAG__index_of__0___model__1_,
                index.GenerateDateTime.ToString("g"),
                index.EmbeddingModel ?? parameters.EmbeddingAgent!.Technical.ChosenModel
                );

            Status = Resources.Resources.RAG__vectorizing_the_query;
            ShowProgress(true);

            var ragOptions = await FreeAIrOptions.DeserializeRagAsync();

            var shortlist = await RagShortlist.BuildAsync(
                index,
                AgentEmbedding.CreateVectorizer(parameters.EmbeddingAgent!),
                parameters.SearchText,
                AgentEmbedding.CreateShortlistOptions(ragOptions),
                cancellationToken
                );

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            Status = string.Format(
                Resources.Resources.RAG__ranking__0__outlines,
                shortlist.IndexedOutlineCount
                );

            SearchTrace.Step(
                $"The shortlist has ranked {shortlist.IndexedOutlineCount} outline(s) and kept {shortlist.Candidates.Count} file(s)."
                );

            if (shortlist.SpaceMismatch)
            {
                //the vectors are of the right length and mean nothing: two unrelated models of the
                //same size look identical to everything except the check sentences
                var mismatchMessage = string.Format(
                    Resources.Resources.RAG__another_model_built_the_index__0_,
                    shortlist.SpaceSimilarity!.Value.ToString("F3")
                    );

                Status = mismatchMessage;

                SearchTrace.Stop(mismatchMessage);

                await VS.MessageBox.ShowErrorAsync(
                    Resources.Resources.Error,
                    mismatchMessage
                    );

                return null;
            }

            if (shortlist.ModelMismatch)
            {
                var message = string.Format(
                    Resources.Resources.RAG__the_agent_does_not_match,
                    shortlist.QueryDimensions,
                    shortlist.IndexDimensions
                    );

                Status = message;

                SearchTrace.Stop(message);

                await VS.MessageBox.ShowErrorAsync(
                    Resources.Resources.Error,
                    message
                    );

                return null;
            }

            //the index stores paths relative to the solution, the solution tree gives absolute ones
            var itemByRelativePath = new Dictionary<string, SolutionHelper.FoundSolutionItem>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in foundRootItems)
            {
                if (string.IsNullOrEmpty(item.SolutionItem.FullPath))
                {
                    continue;
                }

                itemByRelativePath[item.SolutionItem.FullPath.MakeRelativeAgainst(solution.FullPath)] = item;
            }

            _uncoveredFiles = RagShortlist.FindUncovered(
                index,
                itemByRelativePath.Keys
                );
            _uncoveredFiles.Sort(StringComparer.OrdinalIgnoreCase);

            UncoveredText = string.Format(
                Resources.Resources.RAG___0__files_are_out_of_the_index,
                _uncoveredFiles.Count
                );
            UncoveredVisibility = _uncoveredFiles.Count > 0
                ? Visibility.Visible
                : Visibility.Collapsed
                ;

            var result = new List<SolutionHelper.FoundSolutionItem>();

            Candidates.Clear();
            foreach (var candidate in shortlist.Candidates)
            {
                //the index covers the whole solution, while the search may be limited to one
                //project or to a file mask
                if (!itemByRelativePath.TryGetValue(candidate.RelativePath, out var item))
                {
                    continue;
                }

                Candidates.Add(candidate);
                result.Add(item);
            }

            CandidatesHeader = string.Format(
                Resources.Resources.RAG__selected__0__files_of__1_,
                result.Count,
                foundRootItems.Count
                );

            if (result.Count == 0)
            {
                Status = Resources.Resources.RAG__nothing_matched_the_query;

                SearchTrace.Stop(Resources.Resources.RAG__nothing_matched_the_query);
                return null;
            }

            Status = CandidatesHeader;

            return result;
        }

        private void ShowProgress(
            bool indeterminate
            )
        {
            IsProgressIndeterminate = indeterminate;
            ProgressVisibility = Visibility.Visible;
        }

        private void ReportProgress(
            double value,
            double maximum
            )
        {
            IsProgressIndeterminate = false;
            ProgressValue = value;
            ProgressMaximum = maximum;
            ProgressVisibility = Visibility.Visible;
        }

        private void HideProgress(
            )
        {
            ProgressVisibility = Visibility.Collapsed;
            IsProgressIndeterminate = false;
            ProgressValue = 0d;
        }

        /// <summary>
        /// Turns the load of the index into the status line and the progress bar.
        ///
        /// The vectors file is read line by line and reports itself after every one of them, which
        /// is thousands of notifications; only whole megabytes reach the UI.
        /// </summary>
        private sealed class IndexLoadProgress : IProgress<EmbeddingIndexLoadProgress>
        {
            private const double _bytesInMegabyte = 1024d * 1024d;

            private readonly NaturalLanguageResultsViewModel _viewModel;

            private long _reportedMegabytes = -1L;

            public IndexLoadProgress(
                NaturalLanguageResultsViewModel viewModel
                )
            {
                _viewModel = viewModel;
            }

            public void Report(
                EmbeddingIndexLoadProgress value
                )
            {
                if (value.Phase != EmbeddingIndexLoadPhaseEnum.ReadingVectors || value.Total <= 0L)
                {
                    _viewModel.Status = Resources.Resources.RAG__preparing_the_index;
                    _viewModel.ShowProgress(true);
                    return;
                }

                var megabytes = (long)(value.Processed / _bytesInMegabyte);
                if (megabytes == _reportedMegabytes)
                {
                    return;
                }

                _reportedMegabytes = megabytes;

                _viewModel.Status = string.Format(
                    Resources.Resources.RAG__loading_the_index__0___1_MB,
                    megabytes,
                    (long)Math.Ceiling(value.Total / _bytesInMegabyte)
                    );
                _viewModel.ReportProgress(value.Processed, value.Total);
            }
        }

        private static async Task<SolutionItem?> DetermineRootAsync(
            NaturalSearchScopeEnum scope
            )
        {
            switch (scope)
            {
                case NaturalSearchScopeEnum.WholeSolution:
                default:
                    return await VS.Solutions.GetCurrentSolutionAsync();
                case NaturalSearchScopeEnum.CurrentProject:
                    return await VS.Solutions.GetActiveProjectAsync();
            }
        }

        private static void FillFoundItemsByLLMJson(
            string jsonBody,
            ref List<FoundResultItem> foundItems
            )
        {
            try
            {
                jsonBody = Regex.Replace(
                    jsonBody,
"""
\"fullpath\":\s*\"([^]"]*)\"
""",

                    match =>
                    {
                        if (match.Groups.Count == 2)
                        {
                            var path = match.Groups[1].Value;
                            path = path.Replace(@"\", @"\\").Replace(@"\\\\", @"\\");
                            return
$"""
"fullpath": "{path}"
""";
                        }

                        return null;
                    });

                using var document = JsonDocument.Parse(jsonBody);
                var root = document.RootElement;
                var matchesElement = FindMatchesElement(root);
                if (matchesElement is null || matchesElement.Value.ValueKind != JsonValueKind.Array)
                {
                    //the model has answered something else than it has been asked for, and the
                    //search would otherwise report an honest looking `Found 0 items`
                    SearchTrace.Step("The answer carries no `matches` array.");
                }

                if (matchesElement != null && matchesElement.Value.ValueKind == JsonValueKind.Array)
                {
                    var accepted = 0;

                    foreach (var item in matchesElement.Value.EnumerateArray())
                    {
                        if (HasRequiredProperties(item))
                        {
                            accepted++;
                            var match = JsonSerializer.Deserialize<Match>(item);
                            foundItems.Add(
                                new FoundResultItem(
                                    filePath: match.fullpath,
                                    foundText: match.found_text,
                                    reason: match.reason,
                                    confidenceLevel: match.confidence_level,
                                    lineIndex: match.line
                                    )
                                );
                        }
                    }

                    SearchTrace.Step(
                        $"{accepted} match(es) of {matchesElement.Value.GetArrayLength()} in the answer carry every property the search needs."
                        );
                }
            }
            catch (Exception excp)
            {
                SearchTrace.Fail("The answer of the agent cannot be read", excp);
            }
        }


        private static JsonElement? FindMatchesElement(JsonElement element)
        {
            // Если текущий элемент содержит "matches" - возвращаем его
            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty("matches", out var matchesElement))
            {
                return matchesElement;
            }

            // Рекурсивно ищем в дочерних элементах
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        var result = FindMatchesElement(property.Value);
                        if (result.HasValue)
                            return result;
                    }
                    break;

                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        var result = FindMatchesElement(item);
                        if (result.HasValue)
                            return result;
                    }
                    break;
            }

            return null;
        }

        private static readonly string[] _requiredProperties = new[]
        {
            "fullpath", "found_text", "confidence_level", "line", "reason"
        };

        private static bool HasRequiredProperties(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            foreach (var prop in _requiredProperties)
            {
                if (!element.TryGetProperty(prop, out _))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool FindSelectionFromDocumentBody(
            Microsoft.VisualStudio.Text.ITextSnapshot snapshot,
            FoundResultItem foundItem,
            ref int startLineIndex,
            ref int startColumnIndex,
            ref int endLineIndex,
            ref int endColumnIndex
            )
        {
            var documentText = snapshot.GetText();

            var startOffset = documentText.IndexOf(foundItem.FoundText);
            var endOffset = startOffset + foundItem.FoundText.Length;
            if (startOffset < 0)
            {
                return false;
            }

            var startLine = snapshot.GetLineFromPosition(startOffset);
            startLineIndex = startLine.LineNumber;
            startColumnIndex = startOffset - startLine.Start.Position;

            var endLine = snapshot.GetLineFromPosition(endOffset);
            endLineIndex = endLine.LineNumber;
            endColumnIndex = endOffset - endLine.Start.Position;

            return true;
        }

        private static void FindSelectionFromLLMData(
            Microsoft.VisualStudio.Text.ITextSnapshot snapshot,
            FoundResultItem foundItem,
            out int startLineIndex,
            out int startColumnIndex,
            out int startOffset,
            out int endOffset,
            out int endLineIndex,
            out int endColumnIndex
            )
        {
            var snapshotLine = snapshot.GetLineFromLineNumber(foundItem.LineIndex);
            var lineText = snapshotLine.GetText();

            startLineIndex = foundItem.LineIndex;
            startColumnIndex = lineText.IndexOf(foundItem.FoundText);
            if (startColumnIndex < 0)
            {
                startColumnIndex = 0;
            }
            startOffset = snapshotLine.Start.Position + startColumnIndex;
            endOffset = startOffset + foundItem.FoundText.Length;
            var endLine = snapshot.GetLineFromPosition(endOffset);
            endLineIndex = endLine.LineNumber;
            endColumnIndex = endOffset - endLine.Start.Position;
        }

        public static async Task ShowPanelAsync(
            NaturalLanguageSearchParameters parameters
            )
        {
            if (parameters is null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            var pane = await NaturalLanguageResultsToolWindow.ShowAsync();

            //an empty window with a search which never starts is what the user sees when this goes
            //wrong, so it says so instead of dying of a NullReferenceException on the way
            var viewModel = (pane?.Content as FrameworkElement)?.DataContext as NaturalLanguageResultsViewModel;
            if (viewModel is null)
            {
                SearchTrace.Stop(
                    $"The results window has no view model (its content is {pane?.Content?.GetType().Name ?? "nothing"})."
                    );
                return;
            }

            viewModel.SetNewChatAsync(parameters)
                .FileAndForget(nameof(NaturalLanguageResultsViewModel.SetNewChatAsync));
        }
    }


    public class NaturalSearchResults
    {
        public Match[] matches
        {
            get; set;
        }
    }

    public class Match
    {
        public string fullpath
        {
            get; set;
        }
        public string found_text
        {
            get; set;
        }
        public double confidence_level
        {
            get; set;
        }
        public int line
        {
            get; set;
        }
        public string reason
        {
            get; set;
        }
    }


    public sealed class FoundResultItem
    {
        public string FilePath
        {
            get;
        }

        public string FileName
        {
            get;
        }

        public string FoundText
        {
            get;
        }

        public string Reason
        {
            get;
        }

        public double ConfidenceLevel
        {
            get;
        }

        /// <summary>
        /// The same number in words. A bare `73` says nothing on its own — the model is not
        /// measuring anything, it is guessing — while `Medium (73)` tells the user how much of the
        /// list is worth opening without pretending the number is more precise than it is.
        /// </summary>
        public string ConfidenceText => $"{DescribeConfidence(ConfidenceLevel)} ({ConfidenceLevel:F0})";

        private const double HighConfidenceLevel = 70d;
        private const double MediumConfidenceLevel = 40d;

        private static string DescribeConfidence(
            double level
            )
        {
            if (level >= HighConfidenceLevel)
            {
                return FreeAIr.Resources.Resources.Confidence__high;
            }

            if (level >= MediumConfidenceLevel)
            {
                return FreeAIr.Resources.Resources.Confidence__medium;
            }

            return FreeAIr.Resources.Resources.Confidence__low;
        }

        public int LineIndex
        {
            get;
        }


        public FoundResultItem(
            string filePath,
            string foundText,
            string reason,
            double confidenceLevel,
            int lineIndex
            )
        {
            FilePath = filePath;

            FileName = new System.IO.FileInfo(filePath).Name;
            FoundText = foundText;
            Reason = reason;
            ConfidenceLevel = 
                confidenceLevel > 100
                    ? 100
                    : (confidenceLevel < 0
                        ? 0
                        : confidenceLevel)
                    ;
            LineIndex = lineIndex;
        }
    }

    public enum NaturalSearchScopeEnum
    {
        WholeSolution,
        CurrentProject
    }
}
