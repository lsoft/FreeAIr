using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using MarkdownParser.Antlr.Answer.Parts;
using System.ComponentModel.Composition;

namespace MarkdownParser.Antlr.Answer
{
    /// <summary>Turns raw markdown text (typically a chat answer) into a <see cref="ParsedMarkdown"/> ready to render into a WPF flow document.</summary>
    public interface IMarkdownParser
    {
        /// <summary>Parses markdown text into blocks and parts.</summary>
        ParsedMarkdown Parse(string text);
    }

    /// <summary>
    /// MEF-exported <see cref="IMarkdownParser"/> that memoizes the last parse: re-rendering the same
    /// still-streaming answer text repeatedly does not re-run the ANTLR grammar each time.
    /// </summary>
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

        /// <summary>Returns the cached result if <paramref name="text"/> matches the last call, otherwise re-parses via <see cref="DirectMarkdownParser"/>.</summary>
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

    /// <summary>
    /// Runs the generated ANTLR lexer/parser/listener pipeline (<see cref="AnswerMarkdownLexer"/>,
    /// <see cref="AnswerMarkdownParser"/>, <see cref="AnswerMarkdownListener"/>) over one markdown
    /// string. Falls back to a single plain-text paragraph on any grammar failure, so a malformed or
    /// partially-streamed answer never crashes the chat UI.
    /// </summary>
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

        /// <summary>Parses <paramref name="text"/> through the ANTLR grammar, falling back to plain text if parsing throws.</summary>
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

        /// <summary>Wraps the raw text in a single paragraph, used when grammar-based parsing fails.</summary>
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

        /// <summary>Lexes, parses and walks <paramref name="text"/> into <paramref name="md"/>; returns false (rather than throwing) on any grammar error.</summary>
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

        /// <summary>Wires up a fresh lexer/parser pair over the given text.</summary>
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
