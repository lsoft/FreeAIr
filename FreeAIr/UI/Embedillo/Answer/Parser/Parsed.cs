using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.UI.Embedillo.Answer.Parser
{
    /// <summary>
    /// Ordered sequence of parsed answer parts (commands, file references, solution
    /// items, plain text) produced by parsing an Embedillo chat input, and able to
    /// recompose itself back into the prompt string sent to the LLM.
    /// </summary>
    public sealed class Parsed
    {
        private readonly List<IParsedPart> _parts = new();

        /// <summary>
        /// The parts making up this parsed answer, in the order they appeared in the input.
        /// </summary>
        public IReadOnlyList<IParsedPart> Parts => _parts;

        /// <summary>
        /// Adds a parsed part to the end of the sequence.
        /// </summary>
        public void AppendPart(IParsedPart part)
        {
            if (part is null)
            {
                throw new ArgumentNullException(nameof(part));
            }

            _parts.Add(part);
        }

        /// <summary>
        /// Rebuilds the full prompt text by concatenating each part's prompt
        /// representation in order.
        /// </summary>
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
