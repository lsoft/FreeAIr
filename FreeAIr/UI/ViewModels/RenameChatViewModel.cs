using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    /// <summary>
    /// Backs the Rename Chat dialog, letting the user edit a chat's display name shown in the
    /// Chat List tool window.
    /// </summary>
    public sealed class RenameChatViewModel : BaseViewModel
    {
        /// <summary>
        /// The chat name being edited in the dialog.
        /// </summary>
        public string ChatName
        {
            get;
            set;
        }

        /// <summary>
        /// Callback invoked to close the Rename Chat dialog, passing whether the new name should
        /// be applied.
        /// </summary>
        public Action<bool>? CloseWindow
        {
            get;
            set;
        }

        /// <summary>
        /// Confirms the rename and closes the dialog, applying the edited chat name; disabled
        /// while the name is empty.
        /// </summary>
        public ICommand ConfirmRenameCommand
        {
            get
            {
                if (field == null)
                {
                    field = new RelayCommand(
                        a =>
                        {
                            if (CloseWindow is not null)
                            {
                                CloseWindow(true);
                            }

                        },
                        a => !string.IsNullOrEmpty(ChatName)
                        );
                }

                return field;
            }
        }

        /// <summary>
        /// Cancels the rename and closes the dialog, discarding the edited chat name.
        /// </summary>
        public ICommand CancelRenameCommand
        {
            get
            {
                if (field == null)
                {
                    field = new RelayCommand(
                        a =>
                        {
                            if (CloseWindow is not null)
                            {
                                CloseWindow(false);
                            }
                        }
                        );
                }

                return field;
            }
        }

        /// <summary>
        /// Opens the Rename Chat dialog pre-filled with the chat's current name.
        /// </summary>
        public RenameChatViewModel(
            string chatName
            )
        {
            ChatName = chatName;
        }
    }
}
