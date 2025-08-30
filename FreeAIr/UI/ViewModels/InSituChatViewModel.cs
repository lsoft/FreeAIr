using FreeAIr.Commands.Other;
using System.ComponentModel.Composition;
using System.Windows.Input;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    public sealed class InSituChatViewModel : BaseViewModel
    {
        private ICommand _closeCommand;
        private ICommand _openFullWindowCommand;

        public FreeAIr.BLogic.Chat CurrentChat
        {
            get;
        }

        public Action? CloseWindow
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

        public ICommand OpenFullWindowCommand
        {
            get
            {
                if (_openFullWindowCommand is null)
                {
                    _openFullWindowCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            await OpenChatListToolWindowCommand.ExecuteCommandAsync();

                            CloseWindow?.Invoke();
                        });
                }

                return _openFullWindowCommand;
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
                            CloseWindow?.Invoke();
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
