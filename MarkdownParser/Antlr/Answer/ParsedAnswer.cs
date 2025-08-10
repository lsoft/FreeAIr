using MarkdownParser.Antlr.Answer.Blocks;
using MarkdownParser.Antlr.Answer.Parts;
using System.Windows.Documents;

namespace MarkdownParser.Antlr.Answer
{
    public sealed class ParsedAnswer
    {
        private readonly List<IBlock> _blocks = new();
        private readonly IFontSizeProvider _fontSizeProvider;

        public IReadOnlyList<IBlock> Blocks => _blocks;

        public ParsedAnswer(
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

        public void AddTableRow(
            string row
            )
        {
            var table = GetOrCreateTableBlock();
            table.AddRow(row);
        }

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

        public void AddHorizontalRuleBlock(
            BlockUIContainer blockUIContainer
            )
        {
            _ = CreateHorizontalRuleBlock(blockUIContainer);
        }

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

        public void AddBlockquoteBlock(
            )
        {
            _ = CreateBlockquoteBlock();
        }

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

        public void AddParagraphBlock(
            )
        {
            _ = CreateParagraphBlock();
        }

        private ParagraphBlock CreateParagraphBlock()
        {
            var block = new ParagraphBlock(
                _fontSizeProvider
                );
            _blocks.Add(block);

            return block;
        }


        public void AddText(string text)
        {
            var lastBlock = GetTextualBlock();
            if (lastBlock is null)
            {
                lastBlock = GetParagraphBlock();
            }

            lastBlock.AddText(text);
        }

        public void AddXmlNode(string text, string nodeName, string body)
        {
            var lastBlock = GetParagraphBlock();

            lastBlock.AddXmlNode(text, nodeName, body);
        }

        public void AddUrl(string text, string description, string link, string title)
        {
            var lastBlock = GetParagraphBlock();

            lastBlock.AddUrl(text, description, link, title);
        }

        public void AddHeader(int headerLevel, string text)
        {
            var lastBlock = GetParagraphBlock();

            lastBlock.AddHeader(headerLevel, text);
        }

        public void AddCodeBlock(string text, string code)
        {
            var lastBlock = GetParagraphBlock();

            lastBlock.AddCodeBlock(text, code);
        }

        public void AddCodeLine(string text)
        {
            var lastBlock = GetParagraphBlock();

            lastBlock.AddCodeLine(text);
        }

        public void AddImage(string text, string description, string link, string title)
        {
            var lastBlock = GetParagraphBlock();

            lastBlock.AddImage(text, description, link, title);
        }

        private ITextualBlock? GetTextualBlock()
        {
            var block = _blocks[_blocks.Count - 1] as ITextualBlock;
            return block;
        }

        private ParagraphBlock GetParagraphBlock()
        {
            var block = _blocks[_blocks.Count - 1] as ParagraphBlock;

            return block;
        }

        #endregion

        public FlowDocument ConvertToFlowDocument(
            AdditionalCommandContainer? acc,
            bool isInProgress
            )
        {
            var flowDocument = CreateFlowDocument(acc, isInProgress);

            return flowDocument;
        }

        private FlowDocument CreateFlowDocument(AdditionalCommandContainer acc, bool isInProgress)
        {
            var document = new FlowDocument();

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

            return document;
        }

    }
}
