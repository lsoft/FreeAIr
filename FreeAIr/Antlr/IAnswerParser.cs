using FreeAIr.UI.Embedillo;
using FreeAIr.UI.Embedillo.Answer.Parser;
using System.Collections.Generic;

namespace FreeAIr.Antlr
{
    public interface IAnswerParser
    {
        IReadOnlyList<MentionVisualLineGenerator> Generators
        {
            get;
        }

        ParsedAnswer Parse(string answer);
    }
}