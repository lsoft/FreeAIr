using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using MarkdownParser.Antlr.Answer.Parts;
using System.ComponentModel.Composition;

namespace MarkdownParser.Antlr.Answer
{
    public interface IMarkdownParser
    {
        ParsedMarkdown Parse(string text);
    }

    [Export(typeof(IMarkdownParser))]
    [Export(typeof(CachedMarkdownParser))]
    public sealed class CachedMarkdownParser : IMarkdownParser
    {
        private readonly object _locker = new();

        private readonly DirectMarkdownParser _parser;

        private string? _previousText;
        private ParsedMarkdown? _previousMarkdown;

        [ImportingConstructor]
        public CachedMarkdownParser(
            DirectMarkdownParser parser
            )
        {
            if (parser is null)
            {
                throw new ArgumentNullException(nameof(parser));
            }

            _parser = parser;
        }

        public ParsedMarkdown Parse(string text)
        {
            lock (_locker)
            {
                if (_previousText is null || _previousText != text)
                {
                    _previousMarkdown = _parser.Parse(text);
                    _previousText = text;
                }

                return _previousMarkdown;
            }
        }
    }

    [Export(typeof(DirectMarkdownParser))]
    public sealed class DirectMarkdownParser : IMarkdownParser
    {
        private readonly IFontSizeProvider _fontSizeProvider;

        [ImportingConstructor]
        public DirectMarkdownParser(
            IFontSizeProvider fontSizeProvider
            )
        {
            if (fontSizeProvider is null)
            {
                throw new ArgumentNullException(nameof(fontSizeProvider));
            }

            _fontSizeProvider = fontSizeProvider;
        }

        public ParsedMarkdown Parse(string text)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var md = new ParsedMarkdown(
                _fontSizeProvider
                );

            if (!GetMarkdownRepresentationSafely(md, text))
            {
                GetFallbackRepresentation(md, text);
            }

            return md;
        }

        private static void GetFallbackRepresentation(
            ParsedMarkdown md,
            string text
            )
        {
            if (md is null)
            {
                throw new ArgumentNullException(nameof(md));
            }

            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            md.AddParagraphBlock();
            md.AddText(text);
        }

        private static bool GetMarkdownRepresentationSafely(
            ParsedMarkdown md,
            string text
            )
        {
            if (md is null)
            {
                throw new ArgumentNullException(nameof(md));
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
                    md
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
