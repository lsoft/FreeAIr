using FreeAIr.BLogic;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    public sealed class InSituChatViewModel : BaseViewModel
    {
        private ICommand _closeCommand;

        public FreeAIr.BLogic.Chat CurrentChat
        {
            get;
        }

        public Action CloseWindow
        {
            get;
            set;
        }

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

        public ICommand CloseCommand
        {
            get
            {
                if (_closeCommand is null)
                {
                    _closeCommand = new RelayCommand(
                        a =>
                        {
                            CloseWindow();
                        });
                }

                return _closeCommand;
            }
        }

        [ImportingConstructor]
        public InSituChatViewModel(
            FreeAIr.BLogic.Chat currentChat
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
