using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using FreeAIr.UI.Embedillo;
using FreeAIr.UI.Embedillo.Answer.Parser;
using System.Collections.Generic;

namespace FreeAIr.Antlr.Prompt
{
    public sealed class PromptParser : IParser
    {
        private readonly List<MentionVisualLineGenerator> _generators = new();

        public IReadOnlyList<MentionVisualLineGenerator> Generators => _generators;

        public PromptParser(
            params IMentionVisualLineGeneratorFactory[] generatorFactories
            )
        {
            if (generatorFactories is null)
            {
                throw new ArgumentNullException(nameof(generatorFactories));
            }

            foreach (var generatorFactory in generatorFactories)
            {
                var generator = generatorFactory.Create();
                _generators.Add(generator);
            }
        }

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
                _generators
                );
            walker.Walk(listener, tree);

            var parsed = listener.GetParsed();

            return parsed;
        }

        private (PromptMarkdownLexer, PromptMarkdownParser) CreateComponents(string answer)
        {
            var ais = new AntlrInputStream(answer);
            var lexer = new PromptMarkdownLexer(ais);
            var tokens = new CommonTokenStream(lexer);
            var parser = new PromptMarkdownParser(tokens);
            return (lexer, parser);
        }
    }
}
