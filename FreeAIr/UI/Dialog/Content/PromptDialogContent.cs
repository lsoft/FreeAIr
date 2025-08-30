using FreeAIr.BLogic;
using MarkdownParser.Antlr.Answer;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Windows.Documents;

namespace FreeAIr.UI.Dialog.Content
{
    public sealed class PromptDialogContent : DialogContent<UserPrompt>
    {
        private FlowDocument _document;

        public FlowDocument Document
        {
            get => _document;
            private set
            {
                _document = value;

                OnPropertyChanged();
            }
        }

        private PromptDialogContent(
            UserPrompt prompt,
            FlowDocument document
            ) : base(prompt, prompt)
        {
            Document = document;
        }

        public static PromptDialogContent Create(
            UserPrompt prompt,
            AdditionalCommandContainer? acc
            )
        {
            var componentModel = FreeAIrPackage.Instance.GetService<SComponentModel, IComponentModel>();
            var answerParser = componentModel.GetService<IMarkdownParser>();

            var parsedAnswer = answerParser.Parse(prompt.PromptBody);

            var document = new FlowDocument();
            parsedAnswer.UpdateFlowDocument(document, acc, false);

            return new PromptDialogContent(
                prompt,
                document
                );
        }
    }
}