﻿using EnvDTE;
using FreeAIr.BLogic;
using FreeAIr.BLogic.Context;
using FreeAIr.Helper;
using FreeAIr.UI.ToolWindows;
using Microsoft.VisualStudio.ComponentModelHost;

namespace FreeAIr.Commands
{
    [Command(PackageIds.StartDiscussionCommandId)]
    internal sealed class StartDiscussionCommand : BaseCommand<StartDiscussionCommand>
    {
        public StartDiscussionCommand(
            )
        {
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
            var chatContainer = componentModel.GetService<ChatContainer>();

            var std = await TextDescriptorHelper.GetSelectedTextAsync();
            if (std is null || std.SelectedSpan is null)
            {
                await VS.MessageBox.ShowErrorAsync(
                    Resources.Resources.Error,
                    Resources.Resources.Code_NoSelectedCode
                    );
                return;
            }

            var kind = ChatKindEnum.Discussion;

            var chat = chatContainer.StartChat(
                new ChatDescription(
                    kind,
                    null
                    ),
                null
                );

            chat.ChatContext.AddItem(
                new SolutionItemChatContextItem(
                    std.CreateSelectedIdentifier()
                    )
                );

            _ = await ChatListToolWindow.ShowAsync();
        }

    }
}
