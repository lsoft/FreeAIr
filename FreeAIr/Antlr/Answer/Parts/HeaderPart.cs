using System;
using System.Collections.Generic;
using System.Windows.Documents;

namespace FreeAIr.Antlr.Answer.Parts
{
    public sealed class HeaderPart : IPart
    {
        private static readonly int[] _fontSizes = [24, 22, 20, 18, 16, 14];

        public PartTypeEnum Type => PartTypeEnum.Header;

        public int Level
        {
            get;
        }

        public string Text
        {
            get;
        }
        public string Header
        {
            get;
        }

        public HeaderPart(
            int level,
            string header
            )
        {
            if (header is null)
            {
                throw new ArgumentNullException(nameof(header));
            }

            Level = level;
            Text = header;
            Header = header.TrimStart('#');
        }

        public object GetContextForAdditionalCommand()
        {
            return Header;
        }

        public IEnumerable<Inline> GetInlines(bool isInProgress)
        {
            var level = Level - 1;
            if (level >= 6)
            {
                level = 5;
            }

            yield return new Run
            {
                FontSize = _fontSizes[level],
                Text = Header
            };
        }
    }
}
