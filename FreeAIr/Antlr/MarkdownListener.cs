using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using FreeAIr.UI.Embedillo;
using FreeAIr.UI.Embedillo.Answer.Parser;
using System.Collections.Generic;
using System.Linq;

namespace FreeAIr.Antlr
{
    /// <summary>
    /// ANTLR parse-tree listener shared by both the context markdown and prompt markdown grammars.
    /// Walks the leaf rule contexts of the parse tree, turning each word into either a plain string
    /// part or a mention part (file, selection, and so on) resolved through the registered
    /// <see cref="MentionVisualLineGenerator"/> instances, and accumulates the result in a
    /// <see cref="Parsed"/>.
    /// </summary>
    public class MarkdownListener : PromptMarkdownBaseListener
    {
        /// <summary>The mention generators that recognize each anchor symbol (e.g. @-mentions) in the walked markdown.</summary>
        private readonly List<MentionVisualLineGenerator> _generators;
        /// <summary>The answer parts accumulated so far while walking the parse tree, returned by <see cref="GetParsed"/>.</summary>
        private readonly Parsed _parsed;

        /// <summary>
        /// Prepares the listener with the mention generators that recognize the anchor symbols
        /// (such as @-mentions) used in the markdown being walked.
        /// </summary>
        public MarkdownListener(
            List<MentionVisualLineGenerator> generators
            )
        {
            if (generators is null)
            {
                throw new ArgumentNullException(nameof(generators));
            }

            _generators = generators;
            _parsed = new Parsed();
        }

        /// <summary>
        /// Called by the ANTLR walker for every parser rule; only leaf rules (those with no child
        /// rule contexts) are turned into answer parts, since composite rules would otherwise be
        /// processed twice.
        /// </summary>
        public override void EnterEveryRule([NotNull] ParserRuleContext context)
        {
            if (HasChildContexts(context))
            {
                return;
            }

            var word = context.GetText();
            ProcessWord(_parsed, word);
        }

        /// <summary>
        /// Returns the answer parts accumulated so far from walking the parse tree.
        /// </summary>
        public Parsed GetParsed() => _parsed;

        /// <summary>
        /// Classifies one leaf token: if it starts with a registered generator's anchor symbol it
        /// becomes a mention part (e.g. a file or selection reference), otherwise it is appended as
        /// plain text.
        /// </summary>
        private void ProcessWord(
            Parsed parsed,
            string word
            )
        {
            var generator = _generators.FirstOrDefault(
                g => word.StartsWith(g.AnchorSymbol.ToString())
                );
            if (generator is null)
            {
                var part = new StringAnswerPart(word);
                parsed.AppendPart(part);
            }
            else
            {
                var partPayload = word.Substring(1);
                var part = generator.CreatePart(partPayload);
                if (part is null)
                {
                    part = new StringAnswerPart(word);
                }

                parsed.AppendPart(part);
            }
        }

        /// <summary>
        /// Tells whether this rule context has nested rule contexts as children, which marks it as
        /// a composite (non-leaf) rule that <see cref="EnterEveryRule"/> should skip.
        /// </summary>
        private bool HasChildContexts(ParserRuleContext context)
        {
            // Проверяем, есть ли среди дочерних элементов другие контексты
            return context.children?.Any(child => child is ParserRuleContext) ?? false;
        }

    }

}
