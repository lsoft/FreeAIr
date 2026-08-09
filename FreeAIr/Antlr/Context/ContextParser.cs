using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using FreeAIr.UI.Embedillo.Answer.Parser;
using MarkdownParser.Antlr;
using System.Collections.Generic;

namespace FreeAIr.Antlr.Context
{
    /// <summary>
    /// Parses the markdown used to compose chat context (the text built from mentions such as
    /// files, code selections and other context items) into a sequence of answer parts, resolving
    /// each mention through the <see cref="IMentionRecognizer"/> instances it was built with.
    /// </summary>
    public sealed class ContextParser : IParser
    {
        /// <summary>
        /// The mention recognizers this parser knows, one per kind of context mention (file,
        /// selection, and so on) that can appear in the markdown being parsed.
        /// </summary>
        private readonly IReadOnlyList<IMentionRecognizer> _recognizers;

        /// <summary>
        /// Takes the recognizers for the mention kinds the context markdown can contain; the caller
        /// owns them, because in the chat window the very same objects also render the mentions in
        /// the editor.
        /// </summary>
        public ContextParser(
            params IMentionRecognizer[] recognizers
            )
        {
            if (recognizers is null)
            {
                throw new ArgumentNullException(nameof(recognizers));
            }

            _recognizers = recognizers;
        }

        /// <summary>
        /// Runs the ANTLR-generated context markdown lexer and parser over <paramref name="answer"/>
        /// and walks the resulting tree with a <see cref="MarkdownListener"/> to produce the parsed
        /// answer parts. Returns null if the markdown fails to lex or parse.
        /// </summary>
        public Parsed? Parse(string answer)
        {
            if (answer is null)
            {
                throw new ArgumentNullException(nameof(answer));
            }

            var (lexer, parser) = CreateComponents(answer);

            var lexerListener = new ErrorListener<int>();
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(lexerListener);

            var parserListener = new ErrorListener<IToken>();
            parser.RemoveErrorListeners();
            parser.AddErrorListener(parserListener);

            var tree = parser.markdownFile();

            if (lexerListener.HadError || parserListener.HadError)
            {
                return null;
            }

            var walker = new ParseTreeWalker();
            var listener = new MarkdownListener(
                _recognizers
                );
            walker.Walk(listener, tree);

            var parsed = listener.GetParsed();

            return parsed;
        }

        /// <summary>
        /// Wires up the generated context markdown lexer and parser (from the ANTLR grammar) over
        /// the given text so <see cref="Parse"/> can walk the resulting parse tree.
        /// </summary>
        private (ContextMarkdownLexer, ContextMarkdownParser) CreateComponents(string answer)
        {
            var ais = new AntlrInputStream(answer);
            var lexer = new ContextMarkdownLexer(ais);
            var tokens = new CommonTokenStream(lexer);
            var parser = new ContextMarkdownParser(tokens);
            return (lexer, parser);
        }
    }
}
