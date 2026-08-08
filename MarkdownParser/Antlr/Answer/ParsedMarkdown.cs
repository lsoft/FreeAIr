using MarkdownParser.Antlr.Answer.Blocks;
using MarkdownParser.Antlr.Answer.Parts;
using System.Windows.Documents;

namespace MarkdownParser.Antlr.Answer
{
    /// <summary>
    /// The parse result <see cref="AnswerMarkdownListener"/> builds up: an ordered list of
    /// <see cref="IBlock"/>s (paragraph, table, blockquote, horizontal rule) that
    /// <see cref="UpdateFlowDocument"/> turns into WPF <c>Block</c>s for a chat answer's flow document.
    /// </summary>
    public sealed class ParsedMarkdown
    {
        /// <summary>The blocks parsed so far, in document order; the backing store for <see cref="Blocks"/>.</summary>
        private readonly List<IBlock> _blocks = new();
        /// <summary>Supplies the font sizes passed to every block created here.</summary>
        private readonly IFontSizeProvider _fontSizeProvider;

        /// <summary>The parsed blocks, in document order.</summary>
        public IReadOnlyList<IBlock> Blocks => _blocks;

        /// <summary>Creates an empty parse result that will use <paramref name="fontSizeProvider"/> for every block it creates.</summary>
        public ParsedMarkdown(
            IFontSizeProvider fontSizeProvider
            )
        {
            if (fontSizeProvider is null)
            {
                throw new ArgumentNullException(nameof(fontSizeProvider));
            }

            _fontSizeProvider = fontSizeProvider;
        }

        #region adding a table

        /// <summary>Appends a raw markdown table row, reusing the trailing table block or starting a new one.</summary>
        public void AddTableRow(
            string row
            )
        {
            var table = GetOrCreateTableBlock();
            table.AddRow(row);
        }

        /// <summary>Returns the trailing block if it is already a table, otherwise starts and appends a new one.</summary>
        private TableBlock GetOrCreateTableBlock(
            )
        {
            TableBlock table;
            if (_blocks.Count == 0)
            {
                table = new TableBlock(
                    _fontSizeProvider
                    );
                _blocks.Add(table);
            }
            else
            {
                var lastBlock = _blocks[_blocks.Count - 1];
                table = lastBlock as TableBlock;
                if (table is null)
                {
                    table = new TableBlock(
                        _fontSizeProvider
                        );
                    _blocks.Add(table);
                }
            }

            return table;
        }

        #endregion

        #region adding a horizontal rule

        /// <summary>Adds a horizontal-rule block wrapping the given WPF element.</summary>
        public void AddHorizontalRuleBlock(
            BlockUIContainer blockUIContainer
            )
        {
            _ = CreateHorizontalRuleBlock(blockUIContainer);
        }

        /// <summary>Appends a new horizontal-rule block wrapping the given WPF element.</summary>
        private HorizontalRuleBlock CreateHorizontalRuleBlock(
            BlockUIContainer blockUIContainer
            )
        {
            var block = new HorizontalRuleBlock(
                blockUIContainer
                );
            _blocks.Add(block);

            return block;
        }

        #endregion

        #region adding a blockquote

        /// <summary>Starts a new blockquote block.</summary>
        public void AddBlockquoteBlock(
            )
        {
            _ = CreateBlockquoteBlock();
        }

        /// <summary>Appends a new blockquote block.</summary>
        private BlockquoteBlock CreateBlockquoteBlock()
        {
            var block = new BlockquoteBlock(
                _fontSizeProvider
                );
            _blocks.Add(block);

            return block;
        }

        #endregion

        #region adding a paragraph

        /// <summary>Starts a new paragraph block.</summary>
        public void AddParagraphBlock(
            )
        {
            _ = CreateParagraphBlock();
        }

        /// <summary>Appends a new paragraph block.</summary>
        private ParagraphBlock CreateParagraphBlock()
        {
            var block = new ParagraphBlock(
                _fontSizeProvider
                );
            _blocks.Add(block);

            return block;
        }


        /// <summary>Appends plain text to the trailing textual block, or to a new paragraph if the last block isn't textual.</summary>
        public void AddText(string text)
        {
            var lastBlock = GetTextualBlock();
            if (lastBlock is null)
            {
                lastBlock = GetParagraphBlock();
            }

            lastBlock.AddText(text);
        }

        /// <summary>Adds an inline `&lt;tag&gt;body&lt;/tag&gt;` XML node part to the trailing paragraph.</summary>
        public void AddXmlNode(string text, string nodeName, string body)
        {
            var lastBlock = GetParagraphBlock();

            lastBlock.AddXmlNode(text, nodeName, body);
        }

        /// <summary>Adds a `[desc](link "title")` URL part to the trailing paragraph.</summary>
        public void AddUrl(string text, string description, string link, string title)
        {
            var lastBlock = GetParagraphBlock();

            lastBlock.AddUrl(text, description, link, title);
        }

        /// <summary>Adds a header part (levels 1-6) to the trailing paragraph.</summary>
        public void AddHeader(int headerLevel, string text)
        {
            var lastBlock = GetParagraphBlock();

            lastBlock.AddHeader(headerLevel, text);
        }

        /// <summary>Adds a fenced code block part to the trailing paragraph.</summary>
        public void AddCodeBlock(string text, string code)
        {
            var lastBlock = GetParagraphBlock();

            lastBlock.AddCodeBlock(text, code);
        }

        /// <summary>Adds one code line part to the trailing paragraph.</summary>
        public void AddCodeLine(string text)
        {
            var lastBlock = GetParagraphBlock();

            lastBlock.AddCodeLine(text);
        }

        /// <summary>Adds an image part to the trailing paragraph.</summary>
        public void AddImage(string text, string description, string link, string title)
        {
            var lastBlock = GetParagraphBlock();

            lastBlock.AddImage(text, description, link, title);
        }

        /// <summary>Returns the trailing block as an <see cref="ITextualBlock"/> if it is one, otherwise null.</summary>
        private ITextualBlock? GetTextualBlock()
        {
            var block = _blocks[_blocks.Count - 1] as ITextualBlock;
            return block;
        }

        /// <summary>Returns the trailing block cast to <see cref="ParagraphBlock"/>, assumed to be the current paragraph being built.</summary>
        private ParagraphBlock GetParagraphBlock()
        {
            var block = _blocks[_blocks.Count - 1] as ParagraphBlock;

            return block;
        }

        #endregion

        /// <summary>Rebuilds a WPF <see cref="FlowDocument"/> from the parsed blocks, replacing its current contents.</summary>
        public void UpdateFlowDocument(
            FlowDocument document,
            AdditionalCommandContainer? acc,
            bool isInProgress
            )
        {
            document.Blocks.Clear();

            foreach (var block in Blocks)
            {
                var wpfBlock = block.CreateBlock(
                    acc,
                    isInProgress
                    );
                if (wpfBlock is null)
                {
                    continue;
                }

                document.Blocks.Add(wpfBlock);
            }
        }

    }
}
