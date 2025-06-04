using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace FreeAIr.Antlr.Answer
{
    public sealed class ParsedAnswer
    {
        private readonly List<Block> _blocks = new();

        private Block _lastBlock => _blocks.Last();

        public IReadOnlyList<Block> Blocks => _blocks;


        #region adding a block

        public void CreateBlock()
        {
            _blocks.Add(new Block());
        }


        public void SetBlockType(
            BlockTypeEnum blockType,
            BlockUIContainer? blockUIContainer
            )
        {
            _lastBlock.SetType(blockType, blockUIContainer);
        }

        public void AddText(string text)
        {
            _lastBlock.AddText(text);
        }

        public void AddXmlNode(string text, string nodeName, string body)
        {
            _lastBlock.AddXmlNode(text, nodeName, body);
        }

        public void AddUrl(string text, string description, string link, string title)
        {
            _lastBlock.AddUrl(text, description, link, title);
        }

        public void AddHeader(int headerLevel, string text)
        {
            _lastBlock.AddHeader(headerLevel, text);
        }

        public void AddCodeBlock(string text, string code)
        {
            _lastBlock.AddCodeBlock(text, code);
        }

        public void AddCodeLine(string text)
        {
            _lastBlock.AddCodeLine(text);
        }

        public void AddImage(string text, string description, string link, string title)
        {
            _lastBlock.AddImage(text, description, link, title);
        }

        #endregion

        public FlowDocument ConvertToFlowDocument(
            AdditionalCommandContainer? acc,
            bool isInProgress
            )
        {
            var document = new FlowDocument();

            foreach (var block in Blocks)
            {
                var paragraphBlock = block.CreateBlock(
                    acc,
                    isInProgress
                    );

                document.Blocks.Add(paragraphBlock);
            }

            return document;
        }

    }
}
