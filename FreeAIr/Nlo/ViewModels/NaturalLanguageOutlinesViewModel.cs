using EnvDTE80;
using FreeAIr.Helper;
using FreeAIr.NLOutline;
using FreeAIr.Options2.Agent;
using FreeAIr.Options2.Support;
using FreeAIr.Shared.Helper;
using FreeAIr.UI.ToolWindows;
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
    /// <summary>
    /// View model behind the Natural Language Outlines tool window: drives an LLM chat that
    /// generates NLO summary comments for the chosen solution items, lets the user review
    /// and selectively apply them, and shows a diff before writing them into the source files.
    /// </summary>
    [Export(typeof(NaturalLanguageOutlinesViewModel))]
    public sealed class NaturalLanguageOutlinesViewModel : BaseViewModel
    {
        /// <summary>
        /// The chat session used to ask the LLM for NLO comments; started per invocation of
        /// the tool window and stopped/replaced when a new run begins.
        /// </summary>
        private FreeAIr.Chat.Chat? _chat;

        /// <summary>
        /// Human-readable progress text shown in the tool window while comments are being
        /// generated.
        /// </summary>
        private string _status = FreeAIr.Resources.Resources.Idle;

        /// <summary>
        /// Token source used to cancel the in-progress comment generation when the user
        /// invokes <see cref="CancelChatCommand"/>.
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// The currently running <see cref="ProcessSolutionDocumentsAsync"/> task, tracked so
        /// it can be awaited and cancelled from the UI.
        /// </summary>
        private Task? _processingTask;

        /// <summary>
        /// Progress/status text bound to the tool window, reporting how many solution items
        /// have been processed, or the terminal outcome (generated, cancelled, error).
        /// </summary>
        public string Status
        {
            get => _status;
            private set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        /// <summary>
        /// The NLO comments the LLM proposed for the chosen solution items, each with an
        /// `Apply` flag the user toggles before writing them into the source files.
        /// </summary>
        public ObservableCollection2<FoundCommentItem> GeneratedComments
        {
            get;
        }

        /// <summary>
        /// Command that previews a single generated comment: writes a temp copy of its file with
        /// the applied comments inserted and opens the Visual Studio diff viewer against the
        /// original file.
        /// </summary>
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

                return field;
            }
        }

        /// <summary>
        /// Command that writes every comment marked `Apply` into its source file, grouped and
        /// processed per file, then clears the generated comments list.
        /// </summary>
        public ICommand ApplyCommand
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

                return field;
            }
        }

        /// <summary>
        /// Command that cancels the in-progress comment generation, signalling the cancellation
        /// token and awaiting the processing task to actually stop.
        /// </summary>
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

                return field;
            }
        }

        /// <summary>
        /// Creates the view model with an empty generated-comments list.
        /// </summary>
        public NaturalLanguageOutlinesViewModel()
        {
            GeneratedComments = new();
        }

        /// <summary>
        /// Stops any previous chat, starts a fresh no-tool LLM chat for the given agent, and
        /// kicks off <see cref="ProcessSolutionDocumentsAsync"/> in the background to generate
        /// NLO comments for the chosen solution items.
        /// </summary>
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

            var componentModel = await MefHelper.GetComponentModelAsync();
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

        /// <summary>
        /// Drives the NLO generation loop: splits the chosen solution items into
        /// context-size-limited batches, prompts the LLM with the support action's prompt for
        /// each batch, parses the returned natural-language-outline comments, and populates
        /// <see cref="GeneratedComments"/> with the resulting insertable comment lines.
        /// </summary>
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
                        portionSolutionItems.ConvertAll(i => i.SelectedIdentifier.FilePath)
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

        /// <summary>
        /// Rewrites a file's lines with the given comment items inserted at their target lines,
        /// removing any pre-existing single-line comment immediately above, on, or below the
        /// target line first so re-applying does not duplicate comments.
        /// </summary>
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

        /// <summary>
        /// Opens Visual Studio's built-in file diff tool comparing the original file against the
        /// temp file containing the previewed comment insertion.
        /// </summary>
        private static void ShowDiff(FoundCommentItem commentItem, string tempFilePath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var diffFilesCmd = "{5D4C0442-C0A2-4BE8-9B4D-AB1C28450942}";
            var diffFilesId = 256;
            object args = $"\"{commentItem.FileName}\" \"{tempFilePath}\"";

            var dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
            dte.Commands.Raise(diffFilesCmd, diffFilesId, ref args, ref args);
        }


        /// <summary>
        /// Opens the Natural Language Outlines tool window and starts generating NLO comments
        /// for the chosen solution items using the given support action and agent.
        /// </summary>
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

    /// <summary>
    /// A single LLM-proposed NLO comment for one line of one file, shown in the Natural Language
    /// Outlines tool window with a checkbox letting the user include or exclude it before applying.
    /// </summary>
    public sealed class FoundCommentItem : BaseViewModel
    {
        /// <summary>
        /// Backing field for <see cref="Apply"/>.
        /// </summary>
        private bool _apply;

        /// <summary>
        /// Whether this comment should be written into the file when the apply/goto commands run.
        /// </summary>
        public bool Apply
        {
            get => _apply;
            set
            {
                _apply = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Full path of the source file this comment targets.
        /// </summary>
        public string FilePath
        {
            get;
        }

        /// <summary>
        /// File name (without directory) of the source file, used for display and temp-file naming.
        /// </summary>
        public string FileName
        {
            get;
        }

        /// <summary>
        /// The trimmed source line the comment was generated for, shown for context.
        /// </summary>
        public string CommentedLine
        {
            get;
        }

        /// <summary>
        /// The plain-text comment body the LLM proposed.
        /// </summary>
        public string Comment
        {
            get;
        }

        /// <summary>
        /// The fully formatted comment line (language-appropriate comment syntax plus the
        /// original indentation) ready to be inserted into the file.
        /// </summary>
        public string CompleteComment
        {
            get;
        }

        /// <summary>
        /// Zero-based line index in the file where the comment should be inserted.
        /// </summary>
        public int LineIndex
        {
            get;
        }

        /// <summary>
        /// Creates a comment item for the given file, capturing the original line, the LLM's
        /// comment text, the formatted comment to insert, and the target line index.
        /// </summary>
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
