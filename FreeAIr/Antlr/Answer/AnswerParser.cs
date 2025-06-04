using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using System;

namespace FreeAIr.Antlr.Answer
{
    public static class AnswerParser
    {
        public static ParsedAnswer Parse(string text)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var answer = new ParsedAnswer();

            if (!GetMarkdownRepresentationSafely(answer, text))
            {
                GetFallbackRepresentation(answer, text);
            }

            return answer;
        }

        private static void GetFallbackRepresentation(
            ParsedAnswer answer,
            string text
            )
        {
            if (answer is null)
            {
                throw new ArgumentNullException(nameof(answer));
            }

            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            answer.CreateBlock();
            answer.AddText(text);
        }

        private static bool GetMarkdownRepresentationSafely(
            ParsedAnswer answer,
            string text
            )
        {
            if (answer is null)
            {
                throw new ArgumentNullException(nameof(answer));
            }

            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            try
            {
                var (lexer, parser) = CreateComponents(text);

                var lexerListener = new ErrorListener<int>();
                lexer.RemoveErrorListeners();
                lexer.AddErrorListener(lexerListener);

                var parserListener = new ErrorListener<IToken>();
                parser.RemoveErrorListeners();
                parser.AddErrorListener(parserListener);

                var tree = parser.markdownFile();

                var walker = new ParseTreeWalker();
                var listener = new AnswerMarkdownListener(
                    answer
                    );
                walker.Walk(listener, tree);
                return true;
            }
            catch (Exception excp)
            {
                //todo log
            }

            return false;
        }

        private static (AnswerMarkdownLexer, AnswerMarkdownParser) CreateComponents(string answer)
        {
            var ais = new AntlrInputStream(answer);
            var lexer = new AnswerMarkdownLexer(ais);
            var tokens = new CommonTokenStream(lexer);
            var parser = new AnswerMarkdownParser(tokens);
            return (lexer, parser);
        }
    }
}
