using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    public sealed class RenameChatViewModel : BaseViewModel
    {
        public string ChatName
        {
            get;
            set;
        }

        public Action<bool>? CloseWindow
        {
            get;
            set;
        }

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

        public RenameChatViewModel(
            string chatName
            )
        {
            ChatName = chatName;
        }
    }
}
