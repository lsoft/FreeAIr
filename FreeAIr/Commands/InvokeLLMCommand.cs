using Community.VisualStudio.Toolkit;
using EnvDTE;
using EnvDTE80;
using FreeAIr.BLogic;
using FreeAIr.BLogic.Context;
using FreeAIr.BLogic.Context.Composer;
using FreeAIr.Helper;
using FreeAIr.Shared.Helper;
using FreeAIr.UI.Embedillo.Answer.Parser;
using FreeAIr.UI.ToolWindows;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Commands
{
    internal abstract class InvokeLLMCommand<T> : BaseCommand<T>
        where T : InvokeLLMCommand<T>, new()
    {
        protected InvokeLLMCommand(
            )
        {
        }

        protected abstract ChatKindEnum GetChatKind();

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (string.IsNullOrEmpty(ApiPage.Instance.Token))
            {
                await VS.MessageBox.ShowErrorAsync(
                    Resources.Resources.Error,
                    Resources.Resources.Code_NoToken
                    );
                return;
            }

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

            var contextItems = (await ContextComposer.ComposeFromActiveDocumentAsync(
                )).ConvertToChatContextItem();

            var kind = GetChatKind();

            var chat = await chatContainer.StartChatAsync(
                new ChatDescription(
                    kind,
                    null
                    ),
                null,
                null
                );
            if (chat is null)
            {
                return;
            }

            chat.ChatContext.AddItems(
                contextItems
                );

            chat.AddPrompt(
                UserPrompt.CreateCodeBasedPrompt(
                    kind,
                    std.FileName,
                    std.FilePath + std.SelectedSpan.ToString()
                    )
                );

            await ChatListToolWindow.ShowIfEnabledAsync();
        }

    }
}
