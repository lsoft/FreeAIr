using FreeAIr.Embedding;
using Xunit;

namespace FreeAIr.Search.Tests
{
    /// <summary>
    /// What the user is shown when the embedding server says no. `HTTP 400` on its own sends the
    /// user looking through the settings for a fault which the server has already named.
    /// </summary>
    public sealed class EmbeddingFailureFacts
    {
        [Fact]
        public void A_refused_request_names_the_server_the_model_and_the_reason()
        {
            var message = OpenAIEmbeddingVectorizer.BuildFailureMessage(
                400,
                "{\"error\":{\"message\":\"model 'do_not_applied' does not exist\"}}",
                "do_not_applied",
                "http://localhost:5001/v1",
                4
                );

            Assert.Contains("400", message);
            Assert.Contains("http://localhost:5001/v1", message);
            Assert.Contains("do_not_applied", message);
            Assert.Contains("4", message);
            Assert.Contains("does not exist", message);
        }

        [Fact]
        public void A_refusal_with_no_body_says_so_instead_of_trailing_off()
        {
            var message = OpenAIEmbeddingVectorizer.BuildFailureMessage(
                503,
                "   ",
                "bge-m3",
                "http://localhost:5001/v1",
                1
                );

            Assert.Contains("503", message);
            Assert.Contains("no reason", message);
        }

        [Fact]
        public void A_page_of_json_is_cut_short()
        {
            var body = new string('x', 4000);

            var message = OpenAIEmbeddingVectorizer.BuildFailureMessage(
                400,
                body,
                "bge-m3",
                "http://localhost:5001/v1",
                1
                );

            //the whole message has to stay readable in a one line status bar; the interesting part
            //of such an answer is at its front
            Assert.True(
                message.Length < 700,
                $"the message grew to {message.Length} characters"
                );
            Assert.EndsWith("...", message);
        }
    }
}
