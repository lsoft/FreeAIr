using FreeAIr.Llm.OpenAi;
using System.Text;
using Xunit;

namespace FreeAIr.Llm.Tests.OpenAi;

/// <summary>
/// What the reasoning scanner makes of the bytes of a response, one read at a time.
///
/// The transport tests already prove it finds the field in a whole body. These are about the thing
/// a whole body never shows: the reads a real socket hands over do not line up with the lines of
/// the stream, and a scanner which forgot the tail of a read - or decoded each read on its own -
/// would lose a fragment or cut a word in half on a stream which otherwise parses perfectly.
/// </summary>
public sealed class ReasoningSseScannerFacts
{
    [Fact]
    public void ALineSplitAcrossTwoReadsIsStillFound()
    {
        var scanner = new ReasoningSseScanner();
        var body = "data: {\"choices\":[{\"delta\":{\"reasoning_content\":\"halfway\"}}]}\n";

        var first = Feed(scanner, body.Substring(0, 30));
        var second = Feed(scanner, body.Substring(30));

        Assert.Empty(first);
        Assert.Equal(new[] { "halfway" }, second);
    }

    [Fact]
    public void ACharacterSplitAcrossTwoReadsIsNotCorrupted()
    {
        //the two bytes of a cyrillic letter land in different reads, and a decoder created per read
        //turns the first of them into a replacement character
        var scanner = new ReasoningSseScanner();
        var bytes = Encoding.UTF8.GetBytes("data: {\"choices\":[{\"delta\":{\"reasoning\":\"дальше\"}}]}\n");

        var split = Array.IndexOf(bytes, (byte)0xD0) + 1;

        var first = scanner.Append(bytes, 0, split);
        var second = scanner.Append(bytes, split, bytes.Length - split);

        Assert.Empty(first);
        Assert.Equal(new[] { "дальше" }, second);
    }

    [Fact]
    public void SeveralLinesInOneReadComeBackInOrder()
    {
        var scanner = new ReasoningSseScanner();

        var found = Feed(
            scanner,
            "data: {\"choices\":[{\"delta\":{\"reasoning_content\":\"one \"}}]}\n\n"
            + "data: {\"choices\":[{\"delta\":{\"reasoning_content\":\"two\"}}]}\n\n"
            + "data: {\"choices\":[{\"delta\":{\"content\":\"answer\"}}]}\n\n"
            + "data: [DONE]\n\n"
            );

        Assert.Equal(new[] { "one ", "two" }, found);
    }

    [Fact]
    public void ALastLineWithoutALineBreakIsFoundOnFlush()
    {
        var scanner = new ReasoningSseScanner();

        var duringRead = Feed(scanner, "data: {\"choices\":[{\"delta\":{\"reasoning_content\":\"the end\"}}]}");

        Assert.Empty(duringRead);
        Assert.Equal(new[] { "the end" }, scanner.Flush());
        Assert.Empty(scanner.Flush());
    }

    [Fact]
    public void LinesWhichAreNotDataAreIgnored()
    {
        //a server-sent event stream carries more than payloads, and an html error page arrives
        //through the same reads; none of it may look like reasoning
        var scanner = new ReasoningSseScanner();

        var found = Feed(
            scanner,
            "event: message\n"
            + ": a comment\n"
            + "id: 17\n"
            + "<html><body>502 Bad Gateway</body></html>\n"
            + "data: not json at all\n"
            + "data: {\"choices\":[{\"delta\":{}}]}\n"
            );

        Assert.Empty(found);
    }

    [Fact]
    public void CarriageReturnsAreNotPartOfTheReasoning()
    {
        var scanner = new ReasoningSseScanner();

        var found = Feed(scanner, "data: {\"choices\":[{\"delta\":{\"reasoning_content\":\"crlf\"}}]}\r\n");

        Assert.Equal(new[] { "crlf" }, found);
    }

    /// <summary>Hands the text to the scanner as one read, the way a socket would.</summary>
    private static string[] Feed(
        ReasoningSseScanner scanner,
        string text
        )
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return scanner.Append(bytes, 0, bytes.Length).ToArray();
    }
}
