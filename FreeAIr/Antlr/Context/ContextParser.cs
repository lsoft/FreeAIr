﻿using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using FreeAIr.UI.Embedillo;
using FreeAIr.UI.Embedillo.Answer.Parser;
using MarkdownParser.Antlr;
using System.Collections.Generic;

namespace FreeAIr.Antlr.Context
{
    public sealed class ContextParser : IParser
    {
        private readonly List<MentionVisualLineGenerator> _generators = new();

        public IReadOnlyList<MentionVisualLineGenerator> Generators => _generators;

        public ContextParser(
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
