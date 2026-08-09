using FreeAIr.UI.Embedillo.Answer.Parser;

namespace FreeAIr.Antlr
{
    /// <summary>
    /// Common contract for the ANTLR-based markdown parsers (chat context and chat prompt) that
    /// turn raw markdown text with mentions into a sequence of answer parts. A parser only knows
    /// its mentions as <see cref="IMentionRecognizer"/>, so nothing here reaches into the editor
    /// that renders them.
    /// </summary>
    public interface IParser
    {
        /// <summary>
        /// Parses the given markdown text into a <see cref="Parsed"/> result, resolving mentions
        /// through the recognizers the parser was built with.
        /// </summary>
        Parsed Parse(string answer);
    }
}
