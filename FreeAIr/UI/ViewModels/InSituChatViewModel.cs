using FreeAIr.Commands.Other;
using System.ComponentModel.Composition;
using System.Windows.Input;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    /// <summary>
    /// Backs the in-situ chat popup — the lightweight chat window that appears next to the code
    /// editor caret — offering the current chat plus the option to promote it to the full Chat
    /// List tool window.
    /// </summary>
    public sealed class InSituChatViewModel : BaseViewModel
    {
        /// <summary>
        /// The chat displayed in this in-situ popup.
        /// </summary>
        public FreeAIr.Chat.Chat CurrentChat
        {
            get;
        }

        /// <summary>
        /// Callback invoked to close the in-situ chat popup.
        /// </summary>
        public Action? CloseWindow
        {
            get;
            set;
        }

        /// <summary>
        /// Whether the in-situ popup should auto-close when the user switches focus away from it,
        /// mirrored to and from the FreeAIr settings page.
        /// </summary>
        public bool CloseIfSwitchAway
        {
            get
            {
                return UIPage.Instance.CloseIfUserSwitchedAwayFromInSituWindow;
            }

            set
            {
                UIPage.Instance.CloseIfUserSwitchedAwayFromInSituWindow = value;
                UIPage.Instance.Save();
            }
        }

        /// <summary>
        /// Opens the chat in the full Chat List tool window and closes this in-situ popup.
        /// </summary>
        public ICommand OpenFullWindowCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(
                        async a =>
                        {
                            await OpenChatListToolWindowCommand.ExecuteCommandAsync();

                            CloseWindow?.Invoke();
                        });
                }

                return field;
            }
        }

        /// <summary>
        /// Closes the in-situ chat popup without navigating anywhere.
        /// </summary>
        public ICommand CloseCommand
        {
            get
            {
                if (field is null)
                {
                    field = new RelayCommand(
                        a =>
                        {
                            CloseWindow?.Invoke();
                        });
                }

                return field;
            }
        }

        /// <summary>
        /// Creates the in-situ popup view model for the given chat; imported via MEF so the
        /// current chat can be supplied by the hosting command.
        /// </summary>
        [ImportingConstructor]
        public InSituChatViewModel(
            FreeAIr.Chat.Chat currentChat
            )
        {
            if (currentChat is null)
            {
                throw new ArgumentNullException(nameof(currentChat));
            }

            CurrentChat = currentChat;
        }
    }
}
