using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.UI.Embedillo.Answer.Parser
{
    public sealed class Parsed
    {
        private readonly List<IParsedPart> _parts = new();

        public IReadOnlyList<IParsedPart> Parts => _parts;

        public void AppendPart(IParsedPart part)
        {
            if (part is null)
            {
                throw new ArgumentNullException(nameof(part));
            }

            _parts.Add(part);
        }

        public async Task<string> ComposeStringRepresentationAsync()
        {
            if (_parts.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            foreach (var part in _parts)
            {
                var stringPart = await part.AsPromptStringAsync();
                sb.Append(stringPart);
            }

            return sb.ToString();
        }
    }
}
