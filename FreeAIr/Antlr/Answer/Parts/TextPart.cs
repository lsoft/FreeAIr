using System;
using System.Collections.Generic;
using System.Windows.Documents;

namespace FreeAIr.Antlr.Answer.Parts
{
    public sealed class TextPart : IPart
    {
        private List<string> _text;

        public PartTypeEnum Type => PartTypeEnum.Text;

        public string Text => string.Join("", _text);

        public TextPart(
            string text
            )
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            _text = [text];
        }

        public void Append(string text)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            _text.Add(text);
        }

        public object GetContextForAdditionalCommand()
        {
            return Text;
        }

        public IEnumerable<Inline> GetInlines(bool isInProgress)
        {
            yield return new Run
            {
                FontSize = FontSizePage.Instance.TextSize,
                Text = Text
            };
        }
    }
}
