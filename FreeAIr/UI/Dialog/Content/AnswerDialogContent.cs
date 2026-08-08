using MarkdownParser.Antlr.Answer;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Windows;
using System.Windows.Documents;
using FreeAIr.Chat.Content;

namespace FreeAIr.UI.Dialog.Content
{
    /// <summary>
    /// Dialog content that renders one LLM answer bubble in the chat window, converting the answer's
    /// markdown body into a WPF <see cref="FlowDocument"/> and refreshing it as streamed text arrives.
    /// </summary>
    public sealed class AnswerDialogContent : DialogContent<AnswerChatContent>
    {
        /// <summary>Parses the answer's markdown body into renderable parts (code blocks, links, tool calls, etc).</summary>
        private readonly IMarkdownParser _answerParser;
        /// <summary>Commands (e.g. apply-to-file) that markdown parts such as code blocks can attach themselves to.</summary>
        private readonly AdditionalCommandContainer _additionalCommandContainer;

        /// <summary>Answer bubbles are always left-aligned in the chat, unlike the user's own prompts.</summary>
        public HorizontalAlignment HorizontalAlignment => HorizontalAlignment.Left;

        /// <summary>Padding around the answer bubble's border.</summary>
        public Thickness BorderThickness
        {
            get;
        } = new Thickness(10, 1, 1, 1);

        /// <summary>The rendered flow document shown in the chat bubble, rebuilt each time the answer text changes.</summary>
        public FlowDocument Document
        {
            get;
        } = new FlowDocument();

        /// <summary>Wires up the markdown parser and command container, then renders the initial flow document.</summary>
        private AnswerDialogContent(
            IMarkdownParser answerParser,
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
            answer.AnswerChangedEvent.Event += AnswerChangedRaisedAsync;
        }

        /// <summary>Handles the answer's streamed-text-changed event by refreshing the rendered flow document.</summary>
        private async Task AnswerChangedRaisedAsync(object sender, AnswerChangedEventArgs args)
        {
            await UpdateDocumentAsync(true);
        }

        /// <summary>Rebuilds the flow document on the UI thread and notifies bindings that it changed.</summary>
        private async Task UpdateDocumentAsync(bool isInProgress)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            UpdateDocument(isInProgress);

            OnPropertyChanged();
        }

        /// <summary>Reparses the answer's current markdown body and rewrites <see cref="Document"/> from it.</summary>
        private void UpdateDocument(bool isInProgress)
        {
            var parsedAnswer = _answerParser.Parse(TypedContent.AnswerBody);

            parsedAnswer.UpdateFlowDocument(
                Document,
                _additionalCommandContainer,
                isInProgress
                );
        }

        /// <summary>Builds an <see cref="AnswerDialogContent"/> for the given answer, resolving the markdown parser via the VS component model.</summary>
        public static AnswerDialogContent Create(
            AnswerChatContent answer,
            AdditionalCommandContainer? additionalCommandContainer,
            bool isInProgress
            )
        {
            var componentModel = FreeAIrPackage.Instance.GetService<SComponentModel, IComponentModel>();
            var answerParser = componentModel.GetService<IMarkdownParser>();

            return new AnswerDialogContent(
                answerParser,
                additionalCommandContainer,
                answer,
                isInProgress
                );
        }
    }
}