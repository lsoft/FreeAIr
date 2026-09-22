using MarkdownParser.Antlr.Answer;
using MarkdownParser.Antlr.Answer.Parts;
using System.Windows.Controls;
using System.Windows.Documents;
using Xunit;

namespace MarkdownParser.Tests
{
    /// <summary>
    /// How the per-part action buttons cope with a part that cannot produce a command parameter.
    /// The buttons are built while the flow document is, which is while the chat tool window is
    /// being constructed, so one failing part must cost its own button and nothing more.
    /// </summary>
    public class AdditionalCommandFacts
    {
        /// <summary>A part whose command parameter always throws, standing in for an image whose link cannot be loaded.</summary>
        private sealed class ThrowingPart : IPart
        {
            public PartTypeEnum Type => PartTypeEnum.Image;

            public string Text => "![description](diagram.png)";

            public object? GetContextForAdditionalCommand()
            {
                throw new UriFormatException("Invalid URI: The format of the URI could not be determined.");
            }

            public IEnumerable<Inline> GetInlines(bool isInProgress)
            {
                return [];
            }
        }

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

        [Fact]
        public void GetCommandControls_PartCannotProduceItsContext_SkipsTheButtonInsteadOfThrowing()
        {
            StaTestRunner.Run(
                () =>
                {
                    var container = CreateImageCommandContainer();

                    //nothing was buildable, so there is no button strip - and, crucially, no exception
                    Assert.Null(container.GetCommandControls(new ThrowingPart()));
                });
        }

        [Fact]
        public void GetCommandControls_ImagePartWithAnUnloadableLink_StillOffersTheButton()
        {
            StaTestRunner.Run(
                () =>
                {
                    var container = CreateImageCommandContainer();
                    var part = new ImagePart(
                        ConstantFontSizeProvider.Instance,
                        "![description](diagram.png)",
                        "description",
                        "diagram.png",
                        "title"
                        );

                    var controls = container.GetCommandControls(part);

                    //the context is null rather than a bitmap, a parameter the command copes with
                    Assert.NotNull(controls);
                    var border = Assert.IsType<Border>(controls!.Child);
                    var panel = Assert.IsType<StackPanel>(border.Child);
                    var button = Assert.IsType<Button>(Assert.Single(panel.Children));
                    Assert.Null(button.CommandParameter);
                });
        }

        [Fact]
        public void GetCommandControls_NoCommandMatchesThePartType_AnswersNull()
        {
            StaTestRunner.Run(
                () =>
                {
                    var container = CreateImageCommandContainer();
                    var part = new TextPart(ConstantFontSizeProvider.Instance, "plain text");

                    Assert.Null(container.GetCommandControls(part));
                });
        }
    }
}
