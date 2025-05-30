using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using FreeAIr.UI.Embedillo;
using FreeAIr.UI.Embedillo.Answer.Parser;
using System.Collections.Generic;
using System.Linq;

namespace FreeAIr.Antlr
{
    public class MarkdownListener : PromptMarkdownBaseListener
    {
        private readonly List<MentionVisualLineGenerator> _generators;
        private readonly ParsedAnswer _parsedAnswer;

        public MarkdownListener(
            List<MentionVisualLineGenerator> generators
            )
        {
            if (generators is null)
            {
                throw new ArgumentNullException(nameof(generators));
            }

            _generators = generators;
            _parsedAnswer = new ParsedAnswer();
        }

        public override void EnterEveryRule([NotNull] ParserRuleContext context)
        {
            if (HasChildContexts(context))
            {
                return;
            }

            var word = context.GetText();
            ProcessWord(_parsedAnswer, word);
        }

        public ParsedAnswer GetParsedAnswer() => _parsedAnswer;

        private void ProcessWord(
            ParsedAnswer parsedAnswer,
            string word
            )
        {
            var generator = _generators.FirstOrDefault(
                g => word.StartsWith(g.AnchorSymbol.ToString())
                );
            if (generator is null)
            {
                var part = new StringAnswerPart(word);
                parsedAnswer.AppendPart(part);
            }
            else
            {
                var partPayload = word.Substring(1);
                var part = generator.CreatePart(partPayload);
                parsedAnswer.AppendPart(part);
            }
        }

        private bool HasChildContexts(ParserRuleContext context)
        {
            // Проверяем, есть ли среди дочерних элементов другие контексты
            return context.children?.Any(child => child is ParserRuleContext) ?? false;
        }

    }

}
