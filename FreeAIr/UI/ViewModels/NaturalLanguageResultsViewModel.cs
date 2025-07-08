using FreeAIr.BLogic;
using FreeAIr.BLogic.Context;
using FreeAIr.BLogic.Context.Item;
using FreeAIr.Find;
using FreeAIr.Helper;
using FreeAIr.Options2;
using FreeAIr.Options2.Agent;
using FreeAIr.Options2.Support;
using FreeAIr.Shared.Helper;
using FreeAIr.UI.ToolWindows;
using FuzzySharp;
using FuzzySharp.PreProcess;
using Json.Path;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    [Export(typeof(NaturalLanguageResultsViewModel))]
    public sealed class NaturalLanguageResultsViewModel : BaseViewModel
    {
        private static readonly JsonPath _jsonPath = JsonPath.Parse("$..matches.*");

        private Chat? _chat;
        private ICommand _gotoCommand;
        private ICommand _cancelChatCommand;
        
        private string _status = "Idle";

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

        public ObservableCollection2<FoundResultItem> FoundItems
        {
            get;
        }

        public ICommand GotoCommand
        {
            get
            {
                if (_gotoCommand is null)
                {
                    _gotoCommand = new AsyncRelayCommand(
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
                                            "Cannot determine the position of found entry. The file will be open without selection."
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
                                //todo log
                            }
                        }
                        );
                }

                return _gotoCommand;
            }
        }

        public ICommand CancelChatCommand
        {
            get
            {
                if (_cancelChatCommand is null)
                {
                    _cancelChatCommand = new AsyncRelayCommand(
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

                            await processingTask;

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

                return _cancelChatCommand;
            }
        }

        public NaturalLanguageResultsViewModel()
        {
            FoundItems = new();
        }

        public async Task SetNewChatAsync(
            NaturalSearchScopeEnum scope,
            SupportActionJson chosenAction,
            AgentJson chosenAgent,
            string searchText,
            string fileTypesFilterText
            )
        {
            if (chosenAction is null)
            {
                throw new ArgumentNullException(nameof(chosenAction));
            }

            if (chosenAgent is null)
            {
                throw new ArgumentNullException(nameof(chosenAgent));
            }

            if (searchText is null)
            {
                throw new ArgumentNullException(nameof(searchText));
            }

            if (fileTypesFilterText is null)
            {
                throw new ArgumentNullException(nameof(fileTypesFilterText));
            }

            var oldChat = Interlocked.Exchange(ref _chat, null);
            if (oldChat is not null)
            {
                await oldChat.StopAsync();
            }
            _cancellationTokenSource?.Dispose();

            var filesTypeFilters = new FileTypesFilter(
                fileTypesFilterText
                    .Split(';')
                    .ConvertAll(f => new FileTypeFilter(f))
                );

            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
            var chatContainer = componentModel.GetService<ChatContainer>();

            _chat = await chatContainer.StartChatAsync(
                new ChatDescription(
                    null
                    ),
                null,
                await ChatOptions.NoToolAutoProcessedJsonResponseAsync(chosenAgent)
                );
            if (_chat is null)
            {
                //todo messagebox
                return;
            }
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenSource.Token.Register(
                () =>
                {
                    _chat.StopAsync()
                        .FileAndForget(nameof(Chat.StopAsync));
                });

            _processingTask = ProcessSolutionDocumentsAsync(
                scope,
                chosenAction,
                chosenAgent,
                searchText,
                filesTypeFilters
                );
        }

        private async Task ProcessSolutionDocumentsAsync(
            NaturalSearchScopeEnum scope,
            SupportActionJson chosenAction,
            AgentJson chosenAgent,
            string searchText,
            FileTypesFilter filesTypeFilters
            )
        {
            if (chosenAction is null)
            {
                throw new ArgumentNullException(nameof(chosenAction));
            }

            if (searchText is null)
            {
                throw new ArgumentNullException(nameof(searchText));
            }

            if (filesTypeFilters is null)
            {
                throw new ArgumentNullException(nameof(filesTypeFilters));
            }

            var cancellationToken = _cancellationTokenSource.Token;

            var root = await DetermineRootAsync(scope);
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

            FoundItems.Clear();

            List<FoundResultItem> foundItems = new();

            try
            {
                var processedItemCount = 0;

                foreach (var portion in foundRootItems.SplitByItemsSize(chosenAgent.Technical.ContextSize))
                {
                    Status = $"In progress ({processedItemCount}/{foundRootItems.Count})...";

                    cancellationToken.ThrowIfCancellationRequested();

                    var contextItems = new List<IChatContextItem>();

                    foreach (var solutionItem in portion)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var contextItem = new SolutionItemChatContextItem(
                            new UI.Embedillo.Answer.Parser.SelectedIdentifier(
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
                        searchText
                        );

                    var promptText = supportContext.ApplyVariablesToPrompt(
                        chosenAction.Prompt
                        );

                    _chat.AddPrompt(
                        UserPrompt.CreateTextBasedPrompt(
                            promptText
                            )
                        );

                    var cleanAnswer = await _chat.WaitForPromptCleanAnswerAsync(
                        Environment.NewLine
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

                Status = $"Found {FoundItems.Count} items:";
            }
            catch (OperationCanceledException)
            {
                //this is ok
                Status = $"Cancelled";
            }
            catch (Exception excp)
            {
                //todo log
                Status = $"Error: {excp.Message}";
            }

            OnPropertyChanged();
        }

        private static async Task<SolutionItem> DetermineRootAsync(
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

                var parsedJson = JsonNode.Parse(jsonBody);
                var evaluatedJson = _jsonPath.Evaluate(parsedJson);
                foreach (var evaluated in evaluatedJson.Matches)
                {
                    if (evaluated.Value.TryGetSingleValue(out var jsonMatch))
                    {
                        var match = JsonSerializer.Deserialize<Match>(jsonMatch);
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
            }
            catch (Exception excp)
            {
                //todo log
            }
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
            string fileTypesFilterText,
            string subjectToSearchText,
            NaturalSearchScopeEnum chosenScope,
            SupportActionJson chosenAction,
            AgentJson chosenAgent
            )
        {
            if (fileTypesFilterText is null)
            {
                throw new ArgumentNullException(nameof(fileTypesFilterText));
            }

            if (subjectToSearchText is null)
            {
                throw new ArgumentNullException(nameof(subjectToSearchText));
            }

            if (chosenAgent is null)
            {
                throw new ArgumentNullException(nameof(chosenAgent));
            }

            var pane = await NaturalLanguageResultsToolWindow.ShowAsync();
            var toolWindow = pane.Content as NaturalLanguageResultsToolWindowControl;
            var viewModel = toolWindow.DataContext as NaturalLanguageResultsViewModel;
            viewModel.SetNewChatAsync(chosenScope, chosenAction, chosenAgent, subjectToSearchText, fileTypesFilterText)
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
