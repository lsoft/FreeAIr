using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using MarkdownParser.Antlr.Answer.Parts;
using System.ComponentModel.Composition;

namespace MarkdownParser.Antlr.Answer
{
    public interface IAnswerParser
    {
        ParsedAnswer Parse(string text);
    }

    [Export(typeof(IAnswerParser))]
    [Export(typeof(CachedAnswerParser))]
    public sealed class CachedAnswerParser : IAnswerParser
    {
        private readonly object _locker = new();

        private readonly DirectAnswerParser _answerParser;

        private string? _previousText;
        private ParsedAnswer? _previousAnswer;

        [ImportingConstructor]
        public CachedAnswerParser(
            DirectAnswerParser answerParser
            )
        {
            if (answerParser is null)
            {
                throw new ArgumentNullException(nameof(answerParser));
            }

            _answerParser = answerParser;
        }

        public ParsedAnswer Parse(string text)
        {
            lock (_locker)
            {
                if (_previousText is null || _previousText != text)
                {
                    _previousAnswer = _answerParser.Parse(text);
                    _previousText = text;
                }

                return _previousAnswer;
            }
        }
    }

    [Export(typeof(DirectAnswerParser))]
    public sealed class DirectAnswerParser : IAnswerParser
    {
        private readonly IFontSizeProvider _fontSizeProvider;

        [ImportingConstructor]
        public DirectAnswerParser(
            IFontSizeProvider fontSizeProvider
            )
        {
            if (fontSizeProvider is null)
            {
                throw new ArgumentNullException(nameof(fontSizeProvider));
            }

            _fontSizeProvider = fontSizeProvider;
        }

        public ParsedAnswer Parse(string text)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var answer = new ParsedAnswer(
                _fontSizeProvider
                );

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

            answer.AddParagraphBlock();
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
