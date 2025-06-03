using FreeAIr.UI.Embedillo;
using FreeAIr.UI.Embedillo.Answer.Parser;
using System.Collections.Generic;

namespace FreeAIr.Antlr
{
    public interface IParser
    {
        IReadOnlyList<MentionVisualLineGenerator> Generators
        {
            get;
        }

        Parsed Parse(string answer);
    }
}