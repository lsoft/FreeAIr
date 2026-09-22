using MarkdownParser.Antlr.Answer.Parts;
using System.IO;
using System.Windows.Media.Imaging;
using Xunit;

namespace MarkdownParser.Tests
{
    /// <summary>
    /// What an image part does with the link the model wrote. The IMAGE lexer rule accepts anything
    /// without a `)` as the link, so a relative path, an anchor or a plain word all reach here, and
    /// none of them may throw: the copy-to-clipboard button is built while the chat window is being
    /// constructed, and an exception there took the whole tool window down (issue #73).
    ///
    /// Every body runs through <see cref="StaTestRunner"/> - the parts hand back WPF objects, which
    /// only their own thread may then be asked about.
    /// </summary>
    public class ImagePartFacts
    {
        /// <summary>Builds an image part for `![description](link)` the way the markdown listener does.</summary>
        private static ImagePart CreatePart(string link)
        {
            return new ImagePart(
                ConstantFontSizeProvider.Instance,
                "![description](" + link + ")",
                "description",
                link,
                "title"
                );
        }

        /// <summary>A path in the temp directory which is guaranteed not to exist.</summary>
        private static string MissingFilePath()
        {
            return Path.Combine(
                Path.GetTempPath(),
                "freeair-test-" + Guid.NewGuid().ToString("N") + ".png"
                );
        }

        [Theory]
        [InlineData("diagram.png")]
        [InlineData("./img/a.png")]
        [InlineData("img\\a.png")]
        [InlineData("#anchor")]
        [InlineData("")]
        [InlineData("   ")]
        public void GetContextForAdditionalCommand_LinkIsNotAnAbsoluteUri_AnswersNull(string link)
        {
            StaTestRunner.Run(
                () =>
                {
                    var part = CreatePart(link);

                    Assert.Null(part.GetContextForAdditionalCommand());
                });
        }

        [Fact]
        public void GetContextForAdditionalCommand_AbsoluteLinkToAMissingFile_AnswersNull()
        {
            StaTestRunner.Run(
                () =>
                {
                    //BitmapImage opens its source in the constructor, so a missing file throws there
                    var part = CreatePart(MissingFilePath());

                    Assert.Null(part.GetContextForAdditionalCommand());
                });
        }

        [Fact]
        public void GetContextForAdditionalCommand_RootedLinkResolvedAgainstTheCurrentDirectory_AnswersNull()
        {
            StaTestRunner.Run(
                () =>
                {
                    //a `/`-rooted link is combined with the process' current directory, which under
                    //devenv is the IDE's own folder - it resolves to a path, and then to nothing
                    var part = CreatePart("/no/such/image.png");

                    Assert.Null(part.GetContextForAdditionalCommand());
                });
        }

        [Fact]
        public void GetContextForAdditionalCommand_AbsoluteLinkToARealImage_AnswersTheBitmap()
        {
            StaTestRunner.Run(
                () =>
                {
                    using (var png = new TempPngFile())
                    {
                        var part = CreatePart(png.FilePath);

                        var bitmap = Assert.IsType<BitmapImage>(part.GetContextForAdditionalCommand());
                        Assert.Equal(2, bitmap.PixelWidth);
                        Assert.Equal(2, bitmap.PixelHeight);
                    }
                });
        }

        [Fact]
        public void GetInlines_LinkIsNotAnAbsoluteUri_RendersNothing()
        {
            StaTestRunner.Run(
                () =>
                {
                    var part = CreatePart("diagram.png");

                    Assert.Empty(part.GetInlines(false));
                });
        }

        [Fact]
        public void GetInlines_AbsoluteLinkToAMissingFile_RendersNothing()
        {
            StaTestRunner.Run(
                () =>
                {
                    var part = CreatePart(MissingFilePath());

                    Assert.Empty(part.GetInlines(false));
                });
        }

        [Fact]
        public void GetInlines_WhileStreaming_RendersThePlaceholderWithoutTouchingTheLink()
        {
            StaTestRunner.Run(
                () =>
                {
                    var part = CreatePart("diagram.png");

                    Assert.Single(part.GetInlines(true));
                });
        }
    }
}
