using EnvDTE;
using FreeAIr.BLogic;
using FreeAIr.BLogic.Context.Composer;
using FreeAIr.BLogic.Context.Item;
using FreeAIr.Helper;
using FreeAIr.Options2.Support;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.ToolWindows;
using Microsoft.VisualStudio.ComponentModelHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace FreeAIr.Commands.File
{
    [Command(PackageIds.AddFilesToContextCommandId)]
    internal sealed class AddFilesToContextCommand : BaseCommand<AddFilesToContextCommand>
    {
        public AddFilesToContextCommand(
            )
        {
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var viewModel = ChatListToolWindow.ChatListViewModel;
            var selectedChat = viewModel.SelectedChat.Chat;

            var selectedFiles = await ApplyFileSupportCommand.GetSelectedFilesAsync();

            await ApplyFileSupportCommand.AddFilesToContextAsync(
                selectedChat,
                selectedFiles
                );

            await ChatListToolWindow.ShowIfEnabledAsync();
        }

        protected override void BeforeQueryStatus(EventArgs e)
        {
            if (ChatListToolWindow.ChatListViewModel is null)
            {
                this.Command.Enabled = false;
                return;
            }
            if (ChatListToolWindow.ChatListViewModel.SelectedChat is null)
            {
                this.Command.Enabled = false;
                return;
            }

            this.Command.Enabled = true;
        }
    }
}
