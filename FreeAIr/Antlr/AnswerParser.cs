using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using FreeAIr.UI.Embedillo;
using FreeAIr.UI.Embedillo.Answer.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.Antlr
{
    public sealed class AnswerParser
    {
        private readonly List<MentionVisualLineGenerator> _generators;

        public AnswerParser(
            List<MentionVisualLineGenerator> generators
            )
        {
            if (generators is null)
            {
                throw new ArgumentNullException(nameof(generators));
            }

            _generators = generators;
        }

        public ParsedAnswer? Parse(string answer)
        {
            if (answer is null)
            {
                throw new ArgumentNullException(nameof(answer));
            }

            var ais = new AntlrInputStream(answer);

            var lexer = new MDLexer(ais);
            var tokens = new CommonTokenStream(lexer);
            var parser = new MDParser(tokens);

            var lexerListener = new ErrorListener<int>();
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(lexerListener);

            var parserListener = new ErrorListener<IToken>();
            parser.RemoveErrorListeners();
            parser.AddErrorListener(parserListener);

            var tree = parser.markdownFile();

            if (lexerListener.had_error || parserListener.had_error)
            {
                return null;
            }

            var walker = new ParseTreeWalker();
            var listener = new MarkdownListener(
                _generators
                );
            walker.Walk(listener, tree);

            var parsedAnswer = listener.GetParsedAnswer();

            return parsedAnswer;
        }
    }
}
