using FreeAIr.BLogic.Content;
using FreeAIr.Helper;
using FreeAIr.MCP.McpServerProxy;
using FreeAIr.Options2;
using MarkdownParser.Antlr.Answer;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.OLE.Interop;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Management.Instrumentation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace FreeAIr.UI.Dialog.Content
{
    public sealed class AnswerDialogContent : DialogContent<AnswerChatContent>
    {
        private readonly IAnswerParser _answerParser;
        private readonly AdditionalCommandContainer _additionalCommandContainer;

        public HorizontalAlignment HorizontalAlignment => HorizontalAlignment.Left;

        public Thickness BorderThickness
        {
            get;
        } = new Thickness(10, 1, 1, 1);

        public FlowDocument Document
        {
            get;
        } = new FlowDocument();

        private AnswerDialogContent(
            IAnswerParser answerParser,
            AdditionalCommandContainer? additionalCommandContainer,
            AnswerChatContent answer,
            bool isInProgress
            ) : base(answer, answer)
        {
            if (answerParser is null)
            {
                throw new ArgumentNullException(nameof(answerParser));
            }

            _answerParser = answerParser;
            _additionalCommandContainer = additionalCommandContainer;

            UpdateDocument(isInProgress);
            answer.AnswerChangedEvent.Event += AnswerChangedEvent_Event;
        }

        private void AnswerChangedEvent_Event(object sender, AnswerChangedEventArgs args)
        {
            UpdateDocumentAsync(true)
                .FileAndForget(nameof(UpdateDocumentAsync));
        }

        private async Task UpdateDocumentAsync(bool isInProgress)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            UpdateDocument(isInProgress);

            OnPropertyChanged();
        }

        private void UpdateDocument(bool isInProgress)
        {
            var parsedAnswer = _answerParser.Parse(TypedContent.AnswerBody);

            parsedAnswer.UpdateFlowDocument(
                Document,
                _additionalCommandContainer,
                isInProgress
                );
        }

        public static AnswerDialogContent Create(
            AnswerChatContent answer,
            AdditionalCommandContainer? additionalCommandContainer,
            bool isInProgress
            )
        {
            var componentModel = FreeAIrPackage.Instance.GetService<SComponentModel, IComponentModel>();
            var answerParser = componentModel.GetService<IAnswerParser>();

            return new AnswerDialogContent(
                answerParser,
                additionalCommandContainer,
                answer,
                isInProgress
                );
        }
    }
}