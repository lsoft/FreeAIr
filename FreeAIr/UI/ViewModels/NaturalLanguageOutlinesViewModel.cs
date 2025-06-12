using EnvDTE80;
using FreeAIr.Agents;
using FreeAIr.BLogic;
using FreeAIr.BLogic.Context;
using FreeAIr.BLogic.Context.Item;
using FreeAIr.Helper;
using FreeAIr.NLOutline;
using FreeAIr.Shared.Helper;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using WpfHelpers;
using static FreeAIr.Helper.SolutionHelper;

namespace FreeAIr.UI.ViewModels
{
    [Export(typeof(NaturalLanguageOutlinesViewModel))]
    public sealed class NaturalLanguageOutlinesViewModel : BaseViewModel
    {
        private Chat? _chat;
        private ICommand _gotoCommand;
        private ICommand _cancelChatCommand;
        
        private string _status = "Idle";

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
                                        $"No comments for file {commentItem.FileName} applied",
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
                                System.IO.File.WriteAllLines(tempFilePath, bodyLines);

                                ShowDiff(commentItem, tempFilePath);

                                System.IO.File.Delete(tempFilePath);
                            }
                            catch (Exception excp)
                            {
                                //todo log
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
                                        $"No comments applied",
                                        buttons: Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK
                                        );
                                    return;
                                }

                                foreach (var group in groups)
                                {
                                    var fileInfo = new FileInfo(group.Key);

                                    var bodyLines = ApplyCommentsForFile(fileInfo.FullName, group);

                                    System.IO.File.WriteAllLines(fileInfo.FullName, bodyLines);
                                }

                                GeneratedComments.Clear();

                                OnPropertyChanged();
                            }
                            catch (Exception excp)
                            {
                                //todo log
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
            Agent defaultAgent,
            List<SolutionItemChatContextItem> contextItems
            )
        {
            if (defaultAgent is null)
            {
                throw new ArgumentNullException(nameof(defaultAgent));
            }

            if (contextItems is null)
            {
                throw new ArgumentNullException(nameof(contextItems));
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
                    ChatKindEnum.Discussion,
                    null
                    ),
                null,
                FreeAIr.BLogic.ChatOptions.NoToolAutoProcessedJsonResponse(defaultAgent)
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
                defaultAgent,
                contextItems
                );
        }

        public async Task ProcessSolutionDocumentsAsync(
            Agent defaultAgent,
            List<SolutionItemChatContextItem> contextItems
            )
        {
            if (contextItems is null)
            {
                throw new ArgumentNullException(nameof(contextItems));
            }

            var cancellationToken = _cancellationTokenSource.Token;

            GeneratedComments.Clear();

            List<FoundCommentItem> foundItems = new();

            try
            {
                var processedItemCount = 0;

                foreach (var portionContextItems in contextItems.SplitByItemsSize(defaultAgent.Technical.ContextSize))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Status = $"In progress ({processedItemCount}/{contextItems.Count})...";

                    //add subject items (to use line numbers)
                    foreach (var portionContextItem in portionContextItems)
                    {
                        _chat.ChatContext.AddItem(portionContextItem);
                    }

                    //then add context items
                    foreach (var portionContextItem in portionContextItems)
                    {
                        var contextualItems = await portionContextItem.SearchRelatedContextItemsAsync();
                        var textContextualItems = contextualItems
                            .FindAll(i => FileTypeHelper.GetFileType(i.SelectedIdentifier.FilePath) == FileTypeEnum.Text);
                        _chat.ChatContext.AddItems(textContextualItems);
                    }


                    _chat.AddPrompt(
                        UserPrompt.CreateTextBasedPrompt(
$$$"""
Identify the logical sections of the code inside following files and summarize these sections by generating comments:
{{{string.Join(", ", portionContextItems.ConvertAll(s => s.SelectedIdentifier.FilePath))}}}
"""
                            )
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
                    _chat.ChatContext.RemoveItems(portionContextItems);

                    processedItemCount += portionContextItems.Count;
                }

                Status = $"Generated {GeneratedComments.Count} comments:";
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

        private static List<string> ApplyCommentsForFile(
            string filePath,
            IEnumerable<FoundCommentItem> appliedComments
            )
        {
            var bodyLines = System.IO.File.ReadAllLines(filePath).ToList();

            foreach (var appliedComment in appliedComments.Where(c => c.Apply).OrderByDescending(c => c.LineIndex))
            {
                appliedComment.ApplyComment(bodyLines);
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

        public bool ReplaceMode
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

            var prefix = CommentHelper.GetSingleLineCommentSymbol(fileInfo.Extension);
            if (commentedLine.Trim().StartsWith(prefix))
            {
                ReplaceMode = true;
            }
            else
            {
                ReplaceMode = false;
            }
        }

        public void ApplyComment(List<string> bodyLines)
        {
            if (ReplaceMode)
            {
                bodyLines[LineIndex] = CompleteComment;
            }
            else
            {
                bodyLines.Insert(LineIndex, CompleteComment);
            }
        }
    }
}
