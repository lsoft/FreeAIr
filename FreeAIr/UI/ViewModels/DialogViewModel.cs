﻿using FreeAIr.BLogic;
using FreeAIr.BLogic.Context;
using FreeAIr.Helper;
using FreeAIr.Shared.Helper;
using FreeAIr.UI.ContextMenu;
using MarkdownParser.Antlr.Answer;
using MarkdownParser.Antlr.Answer.Parts;
using Microsoft.VisualStudio.ComponentModelHost;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfHelpers;
using static MarkdownParser.UI.Dialog.DialogControl;
using static FreeAIr.UI.ViewModels.ChatListViewModel;

namespace FreeAIr.UI.ViewModels
{
    public class DialogViewModel : BaseViewModel
    {
        private readonly IAnswerParser _answerParser;

        private ChatWrapper? _selectedChat;

        public ObservableCollection2<Replic> Dialog
        {
            get;
        } = new();

        public AdditionalCommandContainer AdditionalCommandContainer
        {
            get;
        } = new();

        public DialogViewModel(ChatContainer chatContainer)
        {
            if (chatContainer is null)
            {
                throw new ArgumentNullException(nameof(chatContainer));
            }

            var componentModel = FreeAIrPackage.Instance.GetService<SComponentModel, IComponentModel>();
            _answerParser = componentModel.GetService<IAnswerParser>();

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
                    () => _selectedChat.Chat,
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

                            var chat = _selectedChat?.Chat;
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

                            var chat = _selectedChat?.Chat;
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

            chatContainer.PromptStateChangedEvent += PromptStateChanged;
        }

        public void UpdateDialog(
            ChatWrapper? selectedChat
            )
        {
            Dialog.Clear();

            _selectedChat = selectedChat;

            if (selectedChat is not null)
            {
                var chat = selectedChat.Chat;
                if (chat is not null)
                {
                    foreach (var prompt in chat.Prompts)
                    {
                        UpdateDialogPrompt(
                            prompt,
                            PromptChangeKindEnum.PromptAdded
                            );
                    }
                }
            }
        }

        private void UpdateDialogPrompt(
            UserPrompt prompt,
            PromptChangeKindEnum kind
            )
        {
            switch (kind)
            {
                case PromptChangeKindEnum.PromptAdded:
                    {
                        var promptReplic = new Replic(
                            _answerParser.Parse(prompt.PromptBody),
                            prompt,
                            true,
                            AdditionalCommandContainer,
                            false
                            );
                        Dialog.Add(promptReplic);

                        var answerReplic = new Replic(
                            _answerParser.Parse(prompt.Answer.GetUserVisibleAnswer()),
                            prompt,
                            false,
                            AdditionalCommandContainer,
                            false
                            );
                        Dialog.Add(answerReplic);
                    }
                    break;
                case PromptChangeKindEnum.AnswerUpdated:
                    {
                        var replic = Dialog.FirstOrDefault(r => r.IsSameTag(prompt) && !r.IsPrompt);
                        if (replic is not null)
                        {
                            replic.Update(
                                _answerParser.Parse(
                                    prompt.Answer.GetUserVisibleAnswer()
                                    ),
                                true
                                );
                        }
                    }
                    break;
                case PromptChangeKindEnum.PromptArchived:
                    break;
                default:
                    break;
            }
        }

        private async void PromptStateChanged(object sender, PromptEventArgs e)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (_selectedChat is not null && ReferenceEquals(_selectedChat.Chat, e.Chat))
                {
                    UpdateDialogPrompt(
                        e.Prompt,
                        e.Kind
                        );
                }

            }
            catch (Exception excp)
            {
                //todo
            }
        }
    }

    public sealed class ChatContextMenuAdditionalCommand : AdditionalCommand
    {
        private readonly Func<Chat?> _chatFunc;
        private readonly ICommand? _actionCommand;

        public ChatContextMenuAdditionalCommand(
            IFontSizeProvider fontSizeProvider,
            Func<Chat?> chatFunc,
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
                    //todo log
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
