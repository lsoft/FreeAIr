using System;
using System.Collections.Generic;
using System.Windows.Documents;
using System.Windows.Media;

namespace FreeAIr.Antlr.Answer.Parts
{
    public sealed class CodeBlockPart : IPart
    {
        private static readonly Brush _foregroundBrush = new SolidColorBrush(Color.FromRgb(0x56, 0x9C, 0xD6));

        public PartTypeEnum Type => PartTypeEnum.CodeBlock;

        public string Text
        {
            get;
        }
        public string Code
        {
            get;
        }

        public CodeBlockPart(
            string code
            )
        {
            if (code is null)
            {
                throw new ArgumentNullException(nameof(code));
            }

            Text = code;
            Code = code.Trim('`');
        }

        public object GetContextForAdditionalCommand()
        {
            return Code;
        }

        public IEnumerable<Inline> GetInlines(bool isInProgress)
        {
            yield return new Run
            {
                FontFamily = new FontFamily("Cascadia Code"),
                Foreground = _foregroundBrush,
                FontSize = 12,
                Text = Code
            };
        }
    }
}
