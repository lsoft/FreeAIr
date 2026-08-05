using MarkdownParser.Antlr.Answer;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Windows.Documents;
using FreeAIr.Chat;

namespace FreeAIr.UI.Dialog.Content
{
    /// <summary>Dialog content that renders one user prompt bubble in the chat window as a pre-parsed WPF <see cref="FlowDocument"/>.</summary>
    public sealed class PromptDialogContent : DialogContent<UserPrompt>
    {
        /// <summary>Backing field for <see cref="Document"/>.</summary>
        private FlowDocument _document;

        /// <summary>The rendered flow document shown in the prompt bubble.</summary>
        public FlowDocument Document
        {
            get => _document;
            private set
            {
                _document = value;

                OnPropertyChanged();
            }
        }

        /// <summary>Stores the prompt together with its already-rendered flow document.</summary>
        private PromptDialogContent(
            UserPrompt prompt,
            FlowDocument document
            ) : base(prompt, prompt)
        {
            Document = document;
        }

        /// <summary>Parses the prompt's markdown body into a flow document via the VS markdown parser service and builds the dialog content.</summary>
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