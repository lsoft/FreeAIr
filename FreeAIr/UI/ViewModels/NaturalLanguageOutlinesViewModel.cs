using EnvDTE80;
using FreeAIr.Helper;
using FreeAIr.NLOutline;
using FreeAIr.Options2.Agent;
using FreeAIr.Options2.Support;
using FreeAIr.Shared.Helper;
using FreeAIr.UI.ToolWindows;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Input;
using WpfHelpers;
using FreeAIr.Chat;
using FreeAIr.Chat.Context;
using FreeAIr.Chat.Context.Item;

namespace FreeAIr.UI.ViewModels
{
    [Export(typeof(NaturalLanguageOutlinesViewModel))]
    public sealed class NaturalLanguageOutlinesViewModel : BaseViewModel
    {
        private FreeAIr.Chat.Chat? _chat;
        private ICommand _gotoCommand;
        private ICommand _cancelChatCommand;
        
        private string _status = FreeAIr.Resources.Resources.Idle;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private Task? _processingTask;
        private ICommand _applyCommand;

        public string Status
        {
            get => _status;
            private set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public ObservableCollection2<FoundCommentItem> GeneratedComments
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
                                var commentItem = a as FoundCommentItem;
                                if (commentItem is null)
                                {
                                    return;
                                }

                                var appliedComments = GeneratedComments
                                    .Where(c => c.FilePath == commentItem.FilePath)
                                    .Where(c => c.Apply)
                                    .OrderByDescending(c => c.LineIndex)
                                    .ToList();
                                if (appliedComments.Count == 0)
                                {
                                    await VS.MessageBox.ShowAsync(
                                        string.Format(FreeAIr.Resources.Resources.No_comments_for_file__0__applied, commentItem.FileName),
                                        buttons: Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK
                                        );
                                    return;
                                }

                                var fileInfo = new FileInfo(commentItem.FilePath);

                                var bodyLines = ApplyCommentsForFile(fileInfo.FullName, appliedComments);

                                var tempFilePath = System.IO.Path.Combine(
                                    System.IO.Path.GetTempPath(),
                                    fileInfo.Name.Substring(0, fileInfo.Name.Length - fileInfo.Extension.Length)
                                        + "."
                                        + Guid.NewGuid().ToString().Substring(0, 8)
                                        + fileInfo.Extension
                                    );
                                System.IO.File.WriteAllLines(tempFilePath, bodyLines, Encoding.UTF8);

                                ShowDiff(commentItem, tempFilePath);

                                System.IO.File.Delete(tempFilePath);
                            }
                            catch (Exception excp)
                            {
                                excp.ActivityLogException();
                            }
                        }
                        );
                }

                return _gotoCommand;
            }
        }

        public ICommand ApplyCommand
        {
            get
            {
                if (_applyCommand is null)
                {
                    _applyCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            try
                            {
                                var groups = (
                                    from comment in GeneratedComments
                                    where comment.Apply
                                    group comment by comment.FilePath into gcomment
                                    select gcomment
                                    ).ToList();
                                if (groups.Count == 0)
                                {
                                    await VS.MessageBox.ShowAsync(
                                        Resources.Resources.No_comments_applied,
                                        buttons: Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK
                                        );
                                    return;
                                }

                                foreach (var group in groups)
                                {
                                    var fileInfo = new FileInfo(group.Key);

                                    var bodyLines = ApplyCommentsForFile(fileInfo.FullName, group);

                                    System.IO.File.WriteAllLines(fileInfo.FullName, bodyLines, Encoding.UTF8);
                                }

                                GeneratedComments.Clear();

                                OnPropertyChanged();
                            }
                            catch (Exception excp)
                            {
                                excp.ActivityLogException();
                            }
                        },
                        a =>
                        {
                            if (GeneratedComments.Count == 0)
                            {
                                return false;
                            }
                            if (GeneratedComments.Count(c => c.Apply) == 0)
                            {
                                return false;
                            }

                            return true;
                        }
                        );
                }

                return _applyCommand;
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

        public NaturalLanguageOutlinesViewModel()
        {
            GeneratedComments = new();
        }

        public async Task SetNewChatAsync(
            SupportActionJson action,
            AgentJson defaultAgent,
            List<SolutionItemChatContextItem> chosenSolutionItems
            )
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (defaultAgent is null)
            {
                throw new ArgumentNullException(nameof(defaultAgent));
            }

            if (chosenSolutionItems is null)
            {
                throw new ArgumentNullException(nameof(chosenSolutionItems));
            }

            var oldChat = Interlocked.Exchange(ref _chat, null);
            if (oldChat is not null)
            {
                await oldChat.StopAsync();
            }
            _cancellationTokenSource?.Dispose();

            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
            var chatContainer = componentModel.GetService<ChatContainer>();


            _chat = await chatContainer.StartChatAsync(
                new ChatDescription(
                    null
                    ),
                null,
                await FreeAIr.Chat.ChatOptions.NoToolAutoProcessedJsonResponseAsync(defaultAgent)
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
                action,
                defaultAgent,
                chosenSolutionItems
                );
        }

        public async Task ProcessSolutionDocumentsAsync(
            SupportActionJson action,
            AgentJson defaultAgent,
            List<SolutionItemChatContextItem> chosenSolutionItems
            )
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (defaultAgent is null)
            {
                throw new ArgumentNullException(nameof(defaultAgent));
            }

            if (chosenSolutionItems is null)
            {
                throw new ArgumentNullException(nameof(chosenSolutionItems));
            }

            var cancellationToken = _cancellationTokenSource.Token;

            GeneratedComments.Clear();

            List<FoundCommentItem> foundItems = new();

            try
            {
                var processedItemCount = 0;

                foreach (var portionSolutionItems in chosenSolutionItems.SplitByItemsSize(defaultAgent.Technical.ContextSize))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Status = string.Format(
                        FreeAIr.Resources.Resources.In_progress___0___1,
                        processedItemCount,
                        chosenSolutionItems.Count
                        );

                    //add subject items (to use line numbers)
                    foreach (var portionSolutionItem in portionSolutionItems)
                    {
                        _chat.ChatContext.AddItem(portionSolutionItem);
                    }

                    //then add context items
                    IReadOnlyList<SolutionItemChatContextItem> textContextualItems = [];
                    foreach (var portionContextItem in portionSolutionItems)
                    {
                        var contextualItems = await portionContextItem.SearchRelatedContextItemsAsync();
                        textContextualItems = contextualItems
                            .FindAll(i => FileTypeHelper.GetFileType(i.SelectedIdentifier.FilePath) == FileTypeEnum.Text);
                    }
                    _chat.ChatContext.AddItems(textContextualItems);

                    var supportContext = await SupportContext.WithContextItemAsync(
                        portionSolutionItems
                        );

                    var promptText = supportContext.ApplyVariablesToPrompt(
                        action.Prompt
                        );
                    _chat.AddPrompt(
                        UserPrompt.CreateTextBasedPrompt(promptText)
                        );

                    var cleanAnswer = await _chat.WaitForPromptCleanAnswerAsync(
                        Environment.NewLine
                        );
                    if (!string.IsNullOrEmpty(cleanAnswer))
                    {
                        var nlmr = NaturalLanguageOutlineCollection.TryParse(
                            cleanAnswer,
                            out var nlm
                            );
                        if (nlmr)
                        {
                            var lastFilePath = string.Empty;
                            List<string> bodyLines = new();
                            nlm.Comments
                                .OrderBy(comment => comment.FilePath)
                                .ForEach(comment =>
                                {
                                    var fileInfo = new FileInfo(comment.FilePath);

                                    if (lastFilePath != comment.FilePath) //optimization; nlm.Comments are sorted
                                    {
                                        bodyLines = System.IO.File.ReadAllLines(fileInfo.FullName).ToList();
                                        lastFilePath = comment.FilePath;
                                    }

                                    var line = comment.Line;
                                    if (bodyLines.Count <= line)
                                    {
                                        return;
                                    }

                                    var existingLine = bodyLines[line];
                                    var trimmedExistingLine = existingLine.TrimStart();
                                    var prefixLength = existingLine.Length - trimmedExistingLine.Length;

                                    var prefix = string.Empty;
                                    if (prefixLength > 0)
                                    {
                                        prefix = existingLine.Substring(0, prefixLength);
                                    }

                                    var commentFormat = CommentHelper.GetSingleLineCommentTemplate(
                                        fileInfo.Extension
                                        ) ?? "// * {0}";

                                    foundItems.Add(
                                        new FoundCommentItem(
                                            fileInfo,
                                            existingLine.Trim(),
                                            comment.Comment,
                                            prefix + string.Format(commentFormat, comment.Comment),
                                            comment.Line
                                            )
                                        );
                                });

                            
                            foundItems = foundItems.OrderByDescending(i => i.FilePath).ToList();

                            GeneratedComments.Clear();
                            GeneratedComments.AddRange(foundItems);
                        }

                        OnPropertyChanged();
                    }

                    _chat.ArchiveAllPrompts();
                    _chat.ChatContext.RemoveItems(portionSolutionItems);
                    _chat.ChatContext.RemoveItems(textContextualItems);

                    processedItemCount += portionSolutionItems.Count;
                }

                Status = string.Format(
                    FreeAIr.Resources.Resources.Generated__0__comments,
                    GeneratedComments.Count
                    );
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

        private static List<string> ApplyCommentsForFile(
            string filePath,
            IEnumerable<FoundCommentItem> appliedComments
            )
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (appliedComments is null)
            {
                throw new ArgumentNullException(nameof(appliedComments));
            }

            var fileInfo = new FileInfo(filePath);
            var prefix = CommentHelper.GetSingleLineCommentSymbol(fileInfo.Extension);

            bool IsCommentLine(string commentedLine)
            {
                return commentedLine.Trim().StartsWith(prefix);
            }

            var bodyLines = System.IO.File.ReadAllLines(filePath).ToList();

            foreach (var appliedComment in appliedComments.Where(c => c.Apply).OrderByDescending(c => c.LineIndex))
            {
                var ln = appliedComment.LineIndex;
                var nln = ln + 1;
                var pln = ln - 1;

                if (nln < bodyLines.Count)
                {
                    if (IsCommentLine(bodyLines[nln]))
                    {
                        bodyLines.RemoveAt(nln);
                    }
                }

                if (IsCommentLine(bodyLines[ln]))
                {
                    bodyLines.RemoveAt(ln);
                }

                bodyLines.Insert(ln, appliedComment.CompleteComment);

                if (pln >= 0)
                {
                    if (IsCommentLine(bodyLines[pln]))
                    {
                        bodyLines.RemoveAt(pln);
                    }
                }
            }

            return bodyLines;
        }

        private static void ShowDiff(FoundCommentItem commentItem, string tempFilePath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var diffFilesCmd = "{5D4C0442-C0A2-4BE8-9B4D-AB1C28450942}";
            var diffFilesId = 256;
            object args = $"\"{commentItem.FileName}\" \"{tempFilePath}\"";

            var dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
            dte.Commands.Raise(diffFilesCmd, diffFilesId, ref args, ref args);
        }


        public static async Task ShowPanelAsync(
            SupportActionJson action,
            AgentJson agent,
            List<SolutionItemChatContextItem> chosenSolutionItems
            )
        {
            var pane = await NaturalLanguageOutlinesToolWindow.ShowAsync();
            var toolWindow = pane.Content as NaturalLanguageOutlinesToolWindowControl;
            var viewModel = toolWindow.DataContext as NaturalLanguageOutlinesViewModel;
            viewModel.SetNewChatAsync(
                action,
                agent,
                chosenSolutionItems
                )
                .FileAndForget(nameof(NaturalLanguageOutlinesViewModel.SetNewChatAsync));
        }

    }

    public sealed class FoundCommentItem : BaseViewModel
    {
        private bool _apply;

        public bool Apply
        {
            get => _apply;
            set
            {
                _apply = value;
                OnPropertyChanged();
            }
        }

        public string FilePath
        {
            get;
        }

        public string FileName
        {
            get;
        }

        public string CommentedLine
        {
            get;
        }

        public string Comment
        {
            get;
        }

        public string CompleteComment
        {
            get;
        }

        public int LineIndex
        {
            get;
        }

        public FoundCommentItem(
            FileInfo fileInfo,
            string commentedLine,
            string comment,
            string completeComment,
            int lineIndex
            )
        {
            FilePath = fileInfo.FullName;

            Apply = true;
            FileName = fileInfo.Name;
            CommentedLine = commentedLine;
            Comment = comment;
            CompleteComment = completeComment;
            LineIndex = lineIndex;
        }
    }
}
