using FreeAIr.Antlr.Answer;
using FreeAIr.Antlr.Answer.Parts;
using FreeAIr.BLogic;
using FreeAIr.BLogic.Context;
using FreeAIr.Helper;
using FreeAIr.UI.Bridge;
using Microsoft.Xaml.Behaviors.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfHelpers;
using static FreeAIr.UI.Dialog.DialogControl;
using static FreeAIr.UI.ViewModels.ChatListViewModel;
using static FreeAIr.UI.ViewModels.SolutionItemsContextMenuCommandBridge;

namespace FreeAIr.UI.ViewModels
{
    public class DialogViewModel : BaseViewModel
    {
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

            #region AdditionalCommandContainer

            AdditionalCommandContainer.AddAdditionalCommand(
                new AdditionalCommand(
                    PartTypeEnum.CodeLine | PartTypeEnum.CodeBlock | PartTypeEnum.Xml | PartTypeEnum.Url,
                    "📋",
                    "Click to copy to clipboard",
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
                    () => _selectedChat.Chat,
                    PartTypeEnum.CodeLine | PartTypeEnum.CodeBlock,
                    "♼",
                    "Choose context item to replace its content with this code part",
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
                                    "Cannot replace context document body. Please replace manually."
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
                    PartTypeEnum.CodeLine | PartTypeEnum.CodeBlock,
                    "♽",
                    "Replace the selected block of the code in the VS document with this code part",
                    new AsyncRelayCommand(
                        async a =>
                        {
                            var codeText = a as string;
                            if (codeText is null)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    FreeAIr.Resources.Resources.Error,
                                    "Cannot replace selected text. Please replace the selected text manually."
                                    );
                                return;
                            }

                            var chat = _selectedChat?.Chat;
                            if (chat is null)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    FreeAIr.Resources.Resources.Error,
                                    "Cannot replace selected text. Please replace the selected text manually."
                                    );
                                return;
                            }

                            var std = chat.Description.SelectedTextDescriptor;
                            if (std is null)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    FreeAIr.Resources.Resources.Error,
                                    "Cannot replace selected text. Please replace the selected text manually."
                                    );
                                return;
                            }

                            if (!std.IsAbleToManipulate)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    FreeAIr.Resources.Resources.Error,
                                    "Cannot replace selected text. Please replace the selected text manually."
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
                    PartTypeEnum.CodeLine | PartTypeEnum.CodeBlock,
                    "🗎",
                    "Create new file with this code part",
                    new AsyncRelayCommand(
                        async a =>
                        {
                            var codeText = a as string;
                            if (codeText is null)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    FreeAIr.Resources.Resources.Error,
                                    "Cannot create a new file. Please create the file manually."
                                    );
                                return;
                            }

                            var solution = await VS.Solutions.GetCurrentSolutionAsync();
                            if (solution is null)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    FreeAIr.Resources.Resources.Error,
                                    "Cannot create a new file. Please create the file manually."
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
                    PartTypeEnum.Image,
                    "📋",
                    "Click to copy to clipboard",
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
                            AnswerParser.Parse(prompt.PromptBody),
                            prompt,
                            true,
                            AdditionalCommandContainer,
                            false
                            );
                        Dialog.Add(promptReplic);

                        var answerReplic = new Replic(
                            AnswerParser.Parse(prompt.Answer.GetUserVisibleAnswer()),
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
                        var replic = Dialog.FirstOrDefault(d => ReferenceEquals(d.Prompt, prompt) && !d.IsPrompt);
                        if (replic is not null)
                        {
                            replic.Update(
                                AnswerParser.Parse(
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
            Func<Chat?> chatFunc,
            PartTypeEnum partType,
            string title,
            string toolTip,
            ICommand? actionCommand,
            Brush? foreground
            ) : base(partType, title, toolTip, null, foreground)
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
                var chat = _chatFunc();
                if (chat is null)
                {
                    return;
                }

                var menuItems = new List<SolutionItemContextMenuItem>();
                foreach (var chatContextItem in chat.ChatContext.Items)
                {
                    menuItems.Add(
                        new SolutionItemContextMenuItem(
                            chatContextItem.ContextUIDescription,
                            _actionCommand,
                            new Tuple<IChatContextItem, object>(
                                chatContextItem,
                                part.GetContextForAdditionalCommand()
                                )
                            )
                        );
                }

                MenuCommandBridge<SolutionItemContextMenuItem>.ShowContextMenu<SolutionItemsContextMenuCommandBridge>(
                    button,
                    menuItems
                    );
            };

            return button;
        }
    }

    public sealed class SolutionItemsContextMenuCommandBridge : MenuCommandBridge<SolutionItemContextMenuItem>
    {
        protected override int GetMenuID() => PackageIds.SolutionItemsContextMenu;

        public sealed class SolutionItemContextMenuItem
        {
            public string Title
            {
                get;
            }

            public ICommand Command
            {
                get;
            }

            public Tuple<IChatContextItem, object> CommandParameter
            {
                get;
            }

            public SolutionItemContextMenuItem(
                string title,
                ICommand command,
                Tuple<IChatContextItem, object> commandParameter
                )
            {
                if (string.IsNullOrEmpty(title))
                {
                    throw new ArgumentException($"'{nameof(title)}' cannot be null or empty.", nameof(title));
                }

                if (command is null)
                {
                    throw new ArgumentNullException(nameof(command));
                }

                if (commandParameter is null)
                {
                    throw new ArgumentNullException(nameof(commandParameter));
                }

                Title = title;
                Command = command;
                CommandParameter = commandParameter;
            }

            public void InvokeCommand()
            {
                if (Command.CanExecute(CommandParameter))
                {
                    Command.Execute(CommandParameter);
                }
            }

            public override string ToString()
            {
                return Title;
            }
        }
    }
}
