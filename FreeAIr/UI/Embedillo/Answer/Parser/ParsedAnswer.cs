using System.Collections.Generic;
using System.Text;

namespace FreeAIr.UI.Embedillo.Answer.Parser
{
    public sealed class ParsedAnswer
    {
        private readonly List<IAnswerPart> _parts = new();

        public IReadOnlyList<IAnswerPart> Parts => _parts;

        public void AppendPart(IAnswerPart part)
        {
            if (part is null)
            {
                throw new ArgumentNullException(nameof(part));
            }

            _parts.Add(part);
        }

        public string ComposeStringRepresentation()
        {
            if (_parts.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            foreach (var part in _parts)
            {
                var stringPart = part.AsPromptString();
                sb.Append(stringPart);
            }

            return sb.ToString();
        }
    }
}
