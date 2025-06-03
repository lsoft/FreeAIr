using FreeAIr.Antlr.Answer.Parts;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace FreeAIr.Antlr.Answer
{
    public sealed class ParsedAnswer
    {
        private readonly List<Paragraph> _paragraphs = new();

        private Paragraph _lastParagraph => _paragraphs.Last();

        public IReadOnlyList<Paragraph> Paragraphs => _paragraphs;


        #region add

        public void CreateParagraph()
        {
            _paragraphs.Add(new Paragraph());
        }

        public void AddText(string text)
        {
            _lastParagraph.AddText(text);
        }

        public void AddXmlNode(string text, string nodeName, string body)
        {
            _lastParagraph.AddXmlNode(text, nodeName, body);
        }

        public void AddUrl(string text, string description, string link, string title)
        {
            _lastParagraph.AddUrl(text, description, link, title);
        }

        public void AddHeader(int headerLevel, string text)
        {
            _lastParagraph.AddHeader(headerLevel, text);
        }

        public void AddCodeBlock(string text)
        {
            _lastParagraph.AddCodeBlock(text);
        }

        public void AddCodeLine(string text)
        {
            _lastParagraph.AddCodeLine(text);
        }

        public void AddImage(string text, string description, string link, string title)
        {
            _lastParagraph.AddImage(text, description, link, title);
        }

        public void AddHorizontalRule(string text)
        {
            _lastParagraph.AddHorizontalRule(text);
        }

        #endregion

        public FlowDocument ConvertToFlowDocument(
            AdditionalCommandContainer? acc,
            bool isInProgress
            )
        {
            var document = new FlowDocument();

            foreach (var paragraph in Paragraphs)
            {
                var paragraphBlock = paragraph.CreateParagraphBlock();
                if (paragraphBlock is not null)
                {
                    document.Blocks.Add(paragraphBlock);
                    continue;
                }

                var block = new System.Windows.Documents.Paragraph
                {
                    Margin = new Thickness(10, 0, 0, 0)
                };

                foreach (var part in paragraph.Parts)
                {
                    foreach (var inline in part.GetInlines(isInProgress))
                    {
                        block.Inlines.Add(inline);
                    }

                    if (acc is not null)
                    {
                        var controlElement = GetCommandControls(acc, part);
                        if (controlElement is not null)
                        {
                            block.Inlines.Add(controlElement);
                        }
                    }
                }

                document.Blocks.Add(block);
            }

            return document;
        }

        private InlineUIContainer? GetCommandControls(
            AdditionalCommandContainer acc,
            IPart part
            )
        {
            if (acc is null)
            {
                throw new ArgumentNullException(nameof(acc));
            }

            if (part is null)
            {
                throw new ArgumentNullException(nameof(part));
            }

            var ourCommands = acc.AdditionalCommands
                .Where(ac => ac.PartType.HasFlag(part.Type))
                .ToList()
                ;

            if (ourCommands.Count <= 0)
            {
                return null;
            }

            var sp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
            };

            foreach (var ourCommand in ourCommands)
            {
                var tb = new TextBlock
                {
                    Margin = new Thickness(2, 0, 2, 0),
                    FontSize = 12,
                    FontFamily = new FontFamily("Cascadia Code"),
                    ToolTip = ourCommand.ToolTip,
                    FontWeight = FontWeights.Bold,
                    Foreground = ourCommand.Foreground,
                    VerticalAlignment = VerticalAlignment.Center,
                    Cursor = Cursors.Hand,
                    Text = ourCommand.Title
                };
                tb.InputBindings.Add(
                    new MouseBinding(
                        ourCommand.ActionCommand,
                        new MouseGesture(MouseAction.LeftClick)
                        )
                    {
                        CommandParameter = part.GetContextForAdditionalCommand()
                    }
                    );
                sp.Children.Add(tb);
            }

            var border = new Border
            {
                Margin = new Thickness(2, 0, 6, 0),
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
                BorderBrush = Brushes.Green,
                Child = sp
            };

            var ic = new InlineUIContainer(border);
            ic.BaselineAlignment = BaselineAlignment.Center;
            return ic;
        }

    }

    public sealed class Paragraph
    {
        private readonly List<IPart> _parts = new();

        public IReadOnlyList<IPart> Parts => _parts;

        public BlockUIContainer? CreateParagraphBlock()
        {
            if (HorizontalRuleParagraph())
            {
                var border = new Border
                {
                    Margin = new Thickness(5, 5, 5, 5),
                    BorderThickness = new Thickness(2),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                    BorderBrush = System.Windows.Media.Brushes.Green,
                };

                var bc = new BlockUIContainer(border);
                //bc.BaselineAlignment = BaselineAlignment.Center;
                return bc;
            }

            return null;
        }

        private bool HorizontalRuleParagraph()
        {
            if (_parts.Count == 1
                && _parts[0].Type == PartTypeEnum.HorizontalRule)
            {
                return true;
            }

            return false;
        }

        public void AddText(string text)
        {
            if (_parts.Count > 0)
            {
                var last = _parts.Last();
                if (last is TextPart lastText)
                {
                    lastText.Append(text);
                    return;
                }
            }

            _parts.Add(new TextPart(text));
        }

        public void AddXmlNode(string text, string nodeName, string body)
        {
            _parts.Add(new XmlNodePart(text, nodeName, body));
        }

        public void AddUrl(string text, string description, string link, string title)
        {
            _parts.Add(new UrlPart(text, description, link, title));
        }

        public void AddHeader(int headerLevel, string text)
        {
            _parts.Add(new HeaderPart(headerLevel, text));
        }

        public void AddCodeBlock(string text)
        {
            _parts.Add(new CodeBlockPart(text));
        }

        public void AddCodeLine(string text)
        {
            _parts.Add(new CodeLinePart(text));
        }

        public void AddImage(string text, string description, string link, string title)
        {
            _parts.Add(new ImagePart(text, description, link, title));
        }

        public void AddHorizontalRule(string text)
        {
            _parts.Add(new HorizontalRulePart(text));
        }
    }
}
