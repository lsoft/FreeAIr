using FreeAIr.Find;
using FreeAIr.Helper;
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
        private ICommand _gotoCommand;
        private ICommand _cancelChatCommand;
        
        private string _status = Resources.Resources.Idle;

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
            NaturalLanguageSearchParameters parameters
            )
        {
            if (parameters is null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            var oldChat = Interlocked.Exchange(ref _chat, null);
            if (oldChat is not null)
            {
                await oldChat.StopAsync();
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

            _chat = await chatContainer.StartChatAsync(
                new ChatDescription(
                    null
                    ),
                null,
                await ChatOptions.NoToolAutoProcessedJsonResponseAsync(parameters.ChosenAgent)
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

            var root = await DetermineRootAsync(parameters.ChosenScope);
            if (root is null)
            {
                return;
            }

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

                //todo неправильно считается размер порции: не учитываются related context items
                //которые будут добавлены в контекст чата по референсам для C#
                //надо добавлять по одному файлу, добавлять все референс-файлы и оценивать размер
                //если размер стал слишком большим - откатывать последний файл и его референс файлы
                //и использовать их на следующей итерации
                foreach (var portion in foundRootItems.SplitByItemsSize(parameters.ChosenAgent.Technical.ContextSize))
                {
                    Status = $"In progress ({processedItemCount}/{foundRootItems.Count})...";

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
                Status = Resources.Resources.Cancelled;
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();

                Status = Resources.Resources.Error + $": {excp.Message}";
            }

            OnPropertyChanged();
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
                if (matchesElement != null && matchesElement.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in matchesElement.Value.EnumerateArray())
                    {
                        if (HasRequiredProperties(item))
                        {
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
                }
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
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
            var toolWindow = pane.Content as NaturalLanguageResultsToolWindowControl;
            var viewModel = toolWindow.DataContext as NaturalLanguageResultsViewModel;
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
