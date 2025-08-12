using MarkdownParser.Antlr.Answer;
using System.Windows;
using System.Windows.Documents;

namespace FreeAIr.UI.Dialog
{
    public sealed class ReplicContent : DialogContent
    {
        private readonly AdditionalCommandContainer? _acc;
        private FlowDocument _document;

        public ParsedAnswer ParsedAnswer
        {
            get;
            private set;
        }

        public HorizontalAlignment HorizontalAlignment
        {
            get;
        }

        public Thickness BorderThickness
        {
            get;
        }

        public FlowDocument Document
        {
            get => _document;
            private set
            {
                _document = value;

                OnPropertyChanged();
            }
        }

        public ReplicContent(
            ParsedAnswer parsedAnswer,
            object tag,
            bool isPrompt,
            AdditionalCommandContainer? acc,
            bool inProgress
            ) : base(isPrompt ? DialogContentTypeEnum.Prompt : DialogContentTypeEnum.LLMAnswer, tag)
        {
            _acc = acc;
            ParsedAnswer = parsedAnswer;
            HorizontalAlignment = isPrompt ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            BorderThickness = isPrompt ? new Thickness(1, 1, 10, 1) : new Thickness(10, 1, 1, 1);
            Document = parsedAnswer.ConvertToFlowDocument(_acc, inProgress);
        }

        public void Update(
            ParsedAnswer parsedAnswer,
            bool inProgress
            )
        {
            ParsedAnswer = parsedAnswer;
            Document = parsedAnswer.ConvertToFlowDocument(_acc, inProgress);
        }
    }
}