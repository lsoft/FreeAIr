using FreeAIr.Commands.Other;
using System.ComponentModel.Composition;
using System.Windows.Input;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    public sealed class InSituChatViewModel : BaseViewModel
    {
        public FreeAIr.Chat.Chat CurrentChat
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
