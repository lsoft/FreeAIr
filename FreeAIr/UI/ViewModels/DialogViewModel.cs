using FreeAIr.Helper;
using FreeAIr.Shared.Helper;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.Dialog;
using FreeAIr.UI.Dialog.Content;
using MarkdownParser.Antlr.Answer;
using MarkdownParser.Antlr.Answer.Parts;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfHelpers;
using FreeAIr.Chat;
using FreeAIr.Chat.Content;
using FreeAIr.Chat.Context;

namespace FreeAIr.UI.ViewModels
{
    /// <summary>
    /// View model backing the chat dialog panel. Turns a <see cref="FreeAIr.Chat.Chat"/>'s
    /// prompts, LLM answers and tool calls into the <see cref="DialogContent"/> items the
    /// dialog list box binds to, and wires up the per-part context menu actions (copy,
    /// expand, replace selection, create file from code block).
    /// </summary>
    public class DialogViewModel : BaseViewModel
    {
        /// <summary>
        /// The chat currently shown in the dialog, or null when no chat is selected.
        /// </summary>
        private FreeAIr.Chat.Chat? _selectedChat;

        /// <summary>
        /// The ordered list of dialog items (prompts, answers, tool calls) rendered in the chat window.
        /// </summary>
        public ObservableCollection2<DialogContent> Dialog
        {
            get;
        } = new();

        /// <summary>
        /// Registry of extra context-menu/toolbar commands (copy to clipboard, replace selection,
        /// create file, expand/collapse) offered on code, XML, URL and image parts of the dialog.
        /// </summary>
        public AdditionalCommandContainer AdditionalCommandContainer
        {
            get;
        } = new();

        /// <summary>
        /// Builds the dialog view model and registers all the additional per-part commands
        /// (copy, expand XML nodes, replace context item body, replace selected text in the
        /// document, create a new file from a code block, copy an image) shown in the chat UI.
        /// </summary>
        public DialogViewModel(
            )
        {
            #region AdditionalCommandContainer

            AdditionalCommandContainer.AddAdditionalCommand(
                new AdditionalCommand(
                    ConstantFontSizeProvider.Instance,
                    PartTypeEnum.Xml,
                    "⤢",
                    "Expand-Collapse",
                    new RelayCommand(
                        a =>
                        {
                            var xmlNodePart = a as XmlNodePart;
                            if (xmlNodePart is null)
                            {
                                return;
                            }

                            xmlNodePart.ExpandOrCollapse();
                        }),
                    null
                    )
                );
            AdditionalCommandContainer.AddAdditionalCommand(
                new AdditionalCommand(
                    ConstantFontSizeProvider.Instance,
                    PartTypeEnum.Xml,
                    "📋",
                    "Click to copy to clipboard",
                    new RelayCommand(
                        a =>
                        {
                            var xmlNodePart = a as XmlNodePart;
                            if (xmlNodePart is null)
                            {
                                return;
                            }

                            if (!string.IsNullOrEmpty(xmlNodePart.Body))
                            {
                                Clipboard.SetText(xmlNodePart.Body);
                            }
                        }),
                    null
                    )
                );
            AdditionalCommandContainer.AddAdditionalCommand(
                new AdditionalCommand(
                    FontSizePage.Instance,
                    PartTypeEnum.CodeLine | PartTypeEnum.CodeBlock | PartTypeEnum.Url,
                    "📋",
                    FreeAIr.Resources.Resources.Click_to_copy_to_clipboard,
                    new RelayCommand(
                        a =>
                        {
                            var code = a as string;
                            if (!string.IsNullOrEmpty(code))
                            {
                                Clipboard.SetText(code);
                            }
                        }),
                    null
                    )
                );
            AdditionalCommandContainer.AddAdditionalCommand(
                new ChatContextMenuAdditionalCommand(
                    FontSizePage.Instance,
                    () => _selectedChat,
                    PartTypeEnum.CodeLine | PartTypeEnum.CodeBlock,
                    "♼",
                    FreeAIr.Resources.Resources.Choose_context_item_to_replace_its,
                    new AsyncRelayCommand(
                        async a =>
                        {
                            if (a is not Tuple<IChatContextItem, object> tuple)
                            {
                                return;
                            }

                            var contextItem = tuple.Item1;

                            var codeText = tuple.Item2.ToString();
                            if (codeText is null)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    FreeAIr.Resources.Resources.Error,
                                    FreeAIr.Resources.Resources.Cannot_replace_context_document_body
                                    );
                                return;
                            }

                            contextItem.ReplaceWithText(
                                codeText
                                );

                            OnPropertyChanged();
                        }
                        ),
                    null
                    )
                );
            AdditionalCommandContainer.AddAdditionalCommand(
                new AdditionalCommand(
                    FontSizePage.Instance,
                    PartTypeEnum.CodeLine | PartTypeEnum.CodeBlock,
                    "♽",
                    FreeAIr.Resources.Resources.Replace_the_selected_block_of_the,
                    new AsyncRelayCommand(
                        async a =>
                        {
                            var codeText = a as string;
                            if (codeText is null)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    FreeAIr.Resources.Resources.Error,
                                    FreeAIr.Resources.Resources.Cannot_replace_selected_text__Please
                                    );
                                return;
                            }

                            var chat = _selectedChat;
                            if (chat is null)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    FreeAIr.Resources.Resources.Error,
                                    FreeAIr.Resources.Resources.Cannot_replace_selected_text__Please
                                    );
                                return;
                            }

                            var std = chat.Description.SelectedTextDescriptor;
                            if (std is null)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    FreeAIr.Resources.Resources.Error,
                                    FreeAIr.Resources.Resources.Cannot_replace_selected_text__Please
                                    );
                                return;
                            }

                            if (!std.IsAbleToManipulate)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    FreeAIr.Resources.Resources.Error,
                                    FreeAIr.Resources.Resources.Cannot_replace_selected_text__Please
                                    );
                                return;
                            }

                            await std.ReplaceOriginalTextWithNewAsync(
                                codeText
                                );
                        },
                        a =>
                        {
                            var codeText = a as string;
                            if (string.IsNullOrEmpty(codeText))
                            {
                                return false;
                            }

                            var chat = _selectedChat;
                            if (chat is null)
                            {
                                return false;
                            }

                            var std = chat.Description.SelectedTextDescriptor;
                            if (std is null)
                            {
                                return false;
                            }

                            if (!std.IsAbleToManipulate)
                            {
                                return false;
                            }

                            return true;
                        }
                        ),
                    null
                    )
                );
            AdditionalCommandContainer.AddAdditionalCommand(
                new AdditionalCommand(
                    FontSizePage.Instance,
                    PartTypeEnum.CodeLine | PartTypeEnum.CodeBlock,
                    "🗎",
                    FreeAIr.Resources.Resources.Create_new_file_with_this_code_part,
                    new AsyncRelayCommand(
                        async a =>
                        {
                            var codeText = a as string;
                            if (codeText is null)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    FreeAIr.Resources.Resources.Error,
                                    FreeAIr.Resources.Resources.Cannot_create_a_new_file__Please
                                    );
                                return;
                            }

                            var solution = await VS.Solutions.GetCurrentSolutionAsync();
                            if (solution is null)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    FreeAIr.Resources.Resources.Error,
                                    FreeAIr.Resources.Resources.Cannot_create_a_new_file__Please
                                    );
                                return;
                            }

                            var sfi = new FileInfo(solution.FullPath);

                            var sfd = new Microsoft.Win32.SaveFileDialog();
                            sfd.InitialDirectory = sfi.Directory.FullName;
                            sfd.FileName = "_.cs";
                            var r = sfd.ShowDialog();
                            if (!r.HasValue || !r.Value)
                            {
                                return;
                            }

                            var targetFilePath = sfd.FileName;

                            var lineEndings = LineEndingHelper.EditorConfig.GetLineEndingFor(targetFilePath);

                            File.WriteAllText(
                                targetFilePath,
                                codeText.WithLineEnding(lineEndings)
                                );
                        }
                        ),
                    null
                    )
                );
            AdditionalCommandContainer.AddAdditionalCommand(
                new AdditionalCommand(
                    FontSizePage.Instance,
                    PartTypeEnum.Image,
                    "📋",
                    FreeAIr.Resources.Resources.Click_to_copy_to_clipboard,
                    new RelayCommand(
                        a =>
                        {
                            var bitmap = a as BitmapImage;
                            if (bitmap is not null)
                            {
                                Clipboard.SetImage(bitmap);
                            }
                        }),
                    null
                    )
                );

            #endregion
        }

        /// <summary>
        /// Switches the dialog to a different chat (or clears it when null): detaches from the
        /// previous chat's content events, rebuilds the dialog list from the new chat's contents,
        /// and subscribes to further additions.
        /// </summary>
        public void UpdateDialog(
            FreeAIr.Chat.Chat? selectedChat
            )
        {
            Dialog.Clear();

            if (_selectedChat is not null)
            {
                _selectedChat.ContentAddedEvent -= ContentAddedRaised;
            }

            _selectedChat = selectedChat;

            if (selectedChat is not null)
            {
                selectedChat.ContentAddedEvent += ContentAddedRaised;

                RewriteDialog();
            }
        }

        /// <summary>
        /// Clears and repopulates the dialog list from every content item currently held by
        /// the selected chat, used when switching the dialog to a different chat.
        /// </summary>
        private void RewriteDialog()
        {
            foreach (var content in _selectedChat.Contents)
            {
                AddDialogContent(content);
            }
        }

        /// <summary>
        /// Handles the selected chat's content-added event by scheduling the new content to be
        /// appended to the dialog on the UI thread, ignoring events from a chat that is no
        /// longer selected.
        /// </summary>
        private void ContentAddedRaised(object sender, ChatContentAddedEventArgs e)
        {
            if (_selectedChat is null || !ReferenceEquals(_selectedChat, e.Chat))
            {
                return;
            }

            AddDialogContentSafelyAsync(e)
                .FileAndForget(nameof(AddDialogContentSafelyAsync));
        }

        /// <summary>
        /// Marshals onto the main thread and appends the newly added chat content to the dialog,
        /// logging any failure to the activity log instead of letting it escape a fire-and-forget task.
        /// </summary>
        private async Task AddDialogContentSafelyAsync(
            ChatContentAddedEventArgs e
            )
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                AddDialogContent(e.ChatContent);
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }
        }

        #region add dialog content

        /// <summary>
        /// Dispatches a chat content item to the matching dialog-builder method based on its
        /// <see cref="IChatContent.Type"/> (prompt, LLM answer or tool call).
        /// </summary>
        private void AddDialogContent(IChatContent content)
        {
            switch (content.Type)
            {
                case ChatContentTypeEnum.Prompt:
                    AddDialogPrompt(content);
                    break;
                case ChatContentTypeEnum.LLMAnswer:
                    AddDialogAnswer(content, false);
                    break;
                case ChatContentTypeEnum.ToolCall:
                    AddDialogToolCall(content);
                    break;
            }
        }

        /// <summary>
        /// Wraps a user prompt content item into a <see cref="PromptDialogContent"/> and adds
        /// it to the dialog.
        /// </summary>
        private void AddDialogPrompt(IChatContent content)
        {
            var p = PromptDialogContent.Create(
                (UserPrompt)content,
                AdditionalCommandContainer
                );
            Dialog.Add(p);
        }

        /// <summary>
        /// Wraps an LLM answer content item into an <see cref="AnswerDialogContent"/>, marking
        /// whether the answer is still streaming, and adds it to the dialog.
        /// </summary>
        private void AddDialogAnswer(IChatContent content, bool isInProgress)
        {
            var a = AnswerDialogContent.Create(
                (AnswerChatContent)content,
                AdditionalCommandContainer,
                isInProgress
                );
            Dialog.Add(a);
        }

        /// <summary>
        /// Wraps a tool call content item into a <see cref="ToolCallDialogContent"/> and adds
        /// it to the dialog.
        /// </summary>
        private void AddDialogToolCall(IChatContent content)
        {
            var tc = new ToolCallDialogContent(
                (ToolCallChatContent)content
                );
            Dialog.Add(tc);
        }

        #endregion
    }

    /// <summary>
    /// An additional command that, when clicked, lets the user pick one of the current chat's
    /// context items and replaces that item's document body with the code from the dialog part
    /// that raised the command. Used for the "replace context item" button on code blocks.
    /// </summary>
    public sealed class ChatContextMenuAdditionalCommand : AdditionalCommand
    {
        /// <summary>
        /// Resolves the chat whose context items should be offered, evaluated lazily each time
        /// the command runs since the selected chat can change between clicks.
        /// </summary>
        private readonly Func<FreeAIr.Chat.Chat?> _chatFunc;

        /// <summary>
        /// The command invoked with the chosen context item and the part's code text once the
        /// user selects which context item to replace.
        /// </summary>
        private readonly ICommand? _actionCommand;

        /// <summary>
        /// Creates the command, storing the chat resolver, the target part types, the button
        /// caption/tooltip and the action to run once a context item is chosen.
        /// </summary>
        public ChatContextMenuAdditionalCommand(
            IFontSizeProvider fontSizeProvider,
            Func<FreeAIr.Chat.Chat?> chatFunc,
            PartTypeEnum partType,
            string title,
            string toolTip,
            ICommand? actionCommand,
            Brush? foreground
            ) : base(fontSizeProvider, partType, title, toolTip, null, foreground)
        {
            if (chatFunc is null)
            {
                throw new ArgumentNullException(nameof(chatFunc));
            }

            _chatFunc = chatFunc;
            _actionCommand = actionCommand;
        }

        /// <summary>
        /// Creates the button for this command and wires its click handler to prompt for a
        /// context item and replace its body, logging any failure to the activity log.
        /// </summary>
        public override UIElement? CreateControl(IPart part)
        {
            var control = base.CreateControl(part);
            if (control is not Button button)
            {
                throw new InvalidOperationException("Expected a Button");
            }

            button.Click += (sender, e) =>
            {
                try
                {
                    ReplaceDocumentBodyAsync(part)
                        .FileAndForget(nameof(ReplaceDocumentBodyAsync));
                }
                catch (Exception excp)
                {
                    excp.ActivityLogException();
                }
            };

            return button;
        }

        /// <summary>
        /// Prompts the user to choose which of the current chat's context items to replace, then
        /// invokes the action command with the chosen item and the clicked part's code text.
        /// </summary>
        private async Task ReplaceDocumentBodyAsync(
            IPart part
            )
        {
            var chat = _chatFunc();
            if (chat is null)
            {
                return;
            }

            var chosenContextItem = await VisualStudioContextMenuCommandBridge.ShowAsync<IChatContextItem>(
                FreeAIr.Resources.Resources.Choose_context_item_to_replace,
                chat.ChatContext.Items
                    .ConvertAll(i => (i.ContextUIDescription, (object)i))
                );
            if (chosenContextItem is null)
            {
                return;
            }

            _actionCommand.Execute(
                new Tuple<IChatContextItem, object>(
                    chosenContextItem,
                    part.GetContextForAdditionalCommand()
                    )
                );
        }
    }

}
