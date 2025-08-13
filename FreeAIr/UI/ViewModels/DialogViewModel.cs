using FreeAIr.BLogic;
using FreeAIr.BLogic.Content;
using FreeAIr.BLogic.Context;
using FreeAIr.Helper;
using FreeAIr.Shared.Helper;
using FreeAIr.UI.BLogic.Reader;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.Dialog;
using FreeAIr.UI.Dialog.Content;
using MarkdownParser.Antlr.Answer;
using MarkdownParser.Antlr.Answer.Parts;
using Microsoft.VisualStudio.ComponentModelHost;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    public class DialogViewModel : BaseViewModel
    {
        private FreeAIr.BLogic.Chat? _selectedChat;

        public ObservableCollection2<DialogContent> Dialog
        {
            get;
        } = new();

        public AdditionalCommandContainer AdditionalCommandContainer
        {
            get;
        } = new();

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

        public void UpdateDialog(
            FreeAIr.BLogic.Chat? selectedChat
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

        private void RewriteDialog()
        {
            foreach (var content in _selectedChat.Contents)
            {
                AddDialogContent(content);
            }
        }

        private void ContentAddedRaised(object sender, ChatContentAddedEventArgs e)
        {
            if (_selectedChat is null || !ReferenceEquals(_selectedChat, e.Chat))
            {
                return;
            }

            AddDialogContentSafelyAsync(e)
                .FileAndForget(nameof(AddDialogContentSafelyAsync));
        }

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

        private void AddDialogPrompt(IChatContent content)
        {
            var p = PromptDialogContent.Create(
                (UserPrompt)content,
                AdditionalCommandContainer
                );
            Dialog.Add(p);
        }

        private void AddDialogAnswer(IChatContent content, bool isInProgress)
        {
            var a = AnswerDialogContent.Create(
                (AnswerChatContent)content,
                AdditionalCommandContainer,
                isInProgress
                );
            Dialog.Add(a);
        }

        private void AddDialogToolCall(IChatContent content)
        {
            var tc = new ToolCallDialogContent(
                (ToolCallChatContent)content
                );
            Dialog.Add(tc);
        }

        #endregion
    }

    public sealed class ChatContextMenuAdditionalCommand : AdditionalCommand
    {
        private readonly Func<FreeAIr.BLogic.Chat?> _chatFunc;
        private readonly ICommand? _actionCommand;

        public ChatContextMenuAdditionalCommand(
            IFontSizeProvider fontSizeProvider,
            Func<FreeAIr.BLogic.Chat?> chatFunc,
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
