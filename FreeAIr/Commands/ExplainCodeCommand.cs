using EnvDTE;
using EnvDTE80;
using FreeAIr.BLogic;
using FreeAIr.Helper;
using FreeAIr.UI.ToolWindows;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Text;
using OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Commands
{
    [Command(PackageIds.ExplainCommandId)]
    internal sealed class ExplainCodeCommand : BaseCommand<ExplainCodeCommand>
    {
        public ExplainCodeCommand(
            )
        {
        }

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

            var (fileName, selectedCode) = await DocumentHelper.GetSelectedTextAsync();
            if (fileName is null || string.IsNullOrEmpty(selectedCode))
            {
                await VS.MessageBox.ShowErrorAsync(
                    Resources.Resources.Error,
                    Resources.Resources.Code_NoSelectedCode
                    );
                return;
            }

            var kind = ChatKindEnum.ExplainCode;

            chatContainer.StartChat(
                new ChatDescription(
                    kind,
                    fileName
                    ),
                UserPrompt.CreateCodeBasedPrompt(kind, fileName, selectedCode)
                );

            if (ResponsePage.Instance.SwitchToTaskWindow)
            {
                _ = await ChatListToolWindow.ShowAsync();
            }
        }

    }


    [Command(PackageIds.GenerateWholeLineSuggestionCommand)]
    internal sealed class GenerateWholeLineSuggestionCommand : BaseCommand<GenerateWholeLineSuggestionCommand>
    {
        public GenerateWholeLineSuggestionCommand(
            )
        {
        }

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

            var documentView = await VS.Documents.GetActiveDocumentViewAsync();
            if (documentView == null)
            {
                //not a text window
                return;
            }

            var textView = documentView.TextView;
            if (textView == null)
            {
                //not a text window
                return;
            }

            var proposalSource = new FreeAIrProposalSource(
                textView
                );

            var caretPosition = textView.Caret.Position.BufferPosition;

            var proposalCollection = await proposalSource.CreateProposalSourceAsync(
                caretPosition
                );

            await SuggestionHijackHelper.ShowAutocompleteAsync(
                textView,
                proposalCollection
                );
        }

    }
}
