using FreeAIr.UI.Embedillo;
using FreeAIr.UI.Embedillo.Answer.Parser;
using System.Collections.Generic;

namespace FreeAIr.Antlr
{
    /// <summary>
    /// Common contract for the ANTLR-based markdown parsers (chat context and chat prompt) that
    /// turn raw markdown text with mentions into a sequence of answer parts.
    /// </summary>
    public interface IParser
    {
        /// <summary>
        /// The mention generators this parser recognizes, one per kind of mention that can appear
        /// in the markdown being parsed.
        /// </summary>
        IReadOnlyList<MentionVisualLineGenerator> Generators
        {
            get;
        }

        /// <summary>
        /// Parses the given markdown text into a <see cref="Parsed"/> result, resolving mentions
        /// through the registered generators.
        /// </summary>
        Parsed Parse(string answer);
    }
}