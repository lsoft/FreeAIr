using MarkdownParser.Antlr.Answer;
using MarkdownParser.Antlr.Answer.Parts;
using System.Windows.Documents;
using Xunit;

namespace MarkdownParser.Tests
{
    /// <summary>
    /// The whole answer-rendering path an LLM answer takes on its way into the chat window: markdown
    /// text through the ANTLR grammar, into blocks and parts, into a flow document with the action
    /// buttons attached. This is the level at which issue #73 was reported - an answer containing a
    /// relative image link, replayed from the persisted chat, threw out of the tool window's
    /// InitializeComponent, and the chat window then would not open on any later start either.
    /// </summary>
    public class AnswerRenderingFacts
    {
        /// <summary>A container holding one button offered for image parts, as the chat window registers it.</summary>
        private static AdditionalCommandContainer CreateImageCommandContainer()
        {
            var container = new AdditionalCommandContainer();
            container.AddAdditionalCommand(
                new AdditionalCommand(
                    ConstantFontSizeProvider.Instance,
                    PartTypeEnum.Image,
                    "📋",
                    "Click to copy to clipboard",
                    null,
                    null
                    )
                );
            return container;
        }

        /// <summary>Parses <paramref name="markdown"/> and renders it into a fresh flow document, as the answer bubble does.</summary>
        private static FlowDocument Render(string markdown, bool isInProgress)
        {
            var parser = new DirectMarkdownParser(ConstantFontSizeProvider.Instance);
            var parsed = parser.Parse(markdown);

            var document = new FlowDocument();
            parsed.UpdateFlowDocument(
                document,
                CreateImageCommandContainer(),
                isInProgress
                );

            return document;
        }

        [Theory]
        [InlineData("Here is the layout:\r\n\r\n![the layout](diagram.png)\r\n")]
        [InlineData("![the layout](./img/diagram.png)\r\n")]
        [InlineData("![the layout](/img/diagram.png)\r\n")]
        [InlineData("![the badge](#anchor)\r\n")]
        public void UpdateFlowDocument_AnswerNamesAnImageThatCannotBeLoaded_RendersInsteadOfThrowing(
            string markdown
            )
        {
            StaTestRunner.Run(
                () =>
                {
                    Assert.NotEmpty(Render(markdown, false).Blocks);
                });
        }

        [Fact]
        public void UpdateFlowDocument_AnswerNamesAnImageThatCannotBeLoaded_KeepsTheSurroundingText()
        {
            StaTestRunner.Run(
                () =>
                {
                    var document = Render("before ![the layout](diagram.png) after\r\n", false);

                    var text = new TextRange(document.ContentStart, document.ContentEnd).Text;

                    Assert.Contains("before", text);
                    Assert.Contains("after", text);
                });
        }

        [Fact]
        public void UpdateFlowDocument_TheSameAnswerWhileStillStreaming_RendersInsteadOfThrowing()
        {
            StaTestRunner.Run(
                () =>
                {
                    var document = Render("Here is the layout:\r\n\r\n![the layout](diagram.png)\r\n", true);

                    Assert.NotEmpty(document.Blocks);
                });
        }
    }
}
