using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ClientModel.Primitives;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Llm.OpenAi
{
    /// <summary>
    /// Collects the reasoning of a thinking model out of one OpenAI compatible response while the
    /// SDK reads it for everything else.
    ///
    /// The seam is a pipeline policy which swaps the response body for a stream that watches the
    /// bytes going past (<see cref="ReasoningSseScanner"/>) and drops what it recognises into a
    /// queue. <see cref="OpenAiChatTransport"/> drains the queue after every update it gets from
    /// the SDK, so reasoning reaches the chat in the order it was sent.
    ///
    /// The one thing this cannot promise is interleaving. The SDK reads ahead, so reasoning which
    /// was sent after a piece of answer text may be handed over before it. That is a fair trade:
    /// every server which sends this field sends all of it before the answer begins, and the
    /// alternative was to stop using the SDK for the protocol entirely.
    /// </summary>
    internal sealed class OpenAiReasoningCapture
    {
        /// <summary>
        /// The fragments seen but not yet handed to the caller. A queue rather than a buffer
        /// because the reading happens on whichever thread the pipeline used and the draining on
        /// the one enumerating the answer.
        /// </summary>
        private readonly ConcurrentQueue<string> _fragments = new ConcurrentQueue<string>();

        /// <summary>
        /// The policy to add to the client's pipeline. It has to observe the response after the
        /// transport produced it, which is what a `PerCall` policy does when it wraps the body on
        /// the way back out.
        /// </summary>
        public PipelinePolicy CreatePolicy()
        {
            return new CapturePolicy(this);
        }

        /// <summary>
        /// The next fragment of reasoning, or false when there is none waiting. Called in a loop
        /// between the SDK's updates.
        /// </summary>
        public bool TryRead(
            out string fragment
            )
        {
            return _fragments.TryDequeue(out fragment);
        }

        /// <summary>Remembers the fragments one read of the body completed.</summary>
        private void Add(
            IReadOnlyList<string> fragments
            )
        {
            for (var i = 0; i < fragments.Count; i++)
            {
                _fragments.Enqueue(fragments[i]);
            }
        }

        /// <summary>
        /// Replaces the response body with the watching stream once the transport has answered.
        /// Everything else about the request is left exactly as it was - a policy which changed the
        /// response would be changing what the SDK parses.
        /// </summary>
        private sealed class CapturePolicy : PipelinePolicy
        {
            private readonly OpenAiReasoningCapture _capture;

            public CapturePolicy(
                OpenAiReasoningCapture capture
                )
            {
                _capture = capture;
            }

            public override void Process(
                PipelineMessage message,
                IReadOnlyList<PipelinePolicy> pipeline,
                int currentIndex
                )
            {
                ProcessNext(message, pipeline, currentIndex);
                Wrap(message);
            }

            public override async ValueTask ProcessAsync(
                PipelineMessage message,
                IReadOnlyList<PipelinePolicy> pipeline,
                int currentIndex
                )
            {
                await ProcessNextAsync(message, pipeline, currentIndex);
                Wrap(message);
            }

            private void Wrap(
                PipelineMessage message
                )
            {
                var response = message?.Response;
                var content = response?.ContentStream;
                if (content is null)
                {
                    //a failed request, or one the pipeline buffered: there is no streamed body to
                    //watch and the failure is reported by the transport from the buffered content
                    return;
                }

                response!.ContentStream = new WatchingStream(content, _capture);
            }
        }

        /// <summary>
        /// A read-only pass-through over the response body which shows every byte to the scanner
        /// before returning it. The SDK sees a stream which behaves exactly as the original one,
        /// which is the point: nothing about its parsing changes.
        ///
        /// Only the array overloads are overridden. This assembly is netstandard2.0 and cannot see
        /// the span ones, whose base implementations forward to these.
        /// </summary>
        private sealed class WatchingStream : Stream
        {
            private readonly Stream _inner;
            private readonly OpenAiReasoningCapture _capture;
            private readonly ReasoningSseScanner _scanner = new ReasoningSseScanner();

            public WatchingStream(
                Stream inner,
                OpenAiReasoningCapture capture
                )
            {
                _inner = inner;
                _capture = capture;
            }

            public override bool CanRead => _inner.CanRead;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => _inner.Length;

            public override long Position
            {
                get => _inner.Position;
                set => throw new NotSupportedException();
            }

            public override int Read(
                byte[] buffer,
                int offset,
                int count
                )
            {
                var read = _inner.Read(buffer, offset, count);
                Observe(buffer, offset, read);
                return read;
            }

            public override async Task<int> ReadAsync(
                byte[] buffer,
                int offset,
                int count,
                CancellationToken cancellationToken
                )
            {
                var read = await _inner.ReadAsync(buffer, offset, count, cancellationToken);
                Observe(buffer, offset, read);
                return read;
            }

            public override void Flush()
            {
                _inner.Flush();
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

            public override void SetLength(long value) => throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            protected override void Dispose(
                bool disposing
                )
            {
                if (disposing)
                {
                    //the body ended: a server which did not terminate its last line still sent it
                    _capture.Add(_scanner.Flush());
                    _inner.Dispose();
                }

                base.Dispose(disposing);
            }

            /// <summary>Shows the bytes just read to the scanner, and a read of zero as the end of the body.</summary>
            private void Observe(
                byte[] buffer,
                int offset,
                int read
                )
            {
                if (read > 0)
                {
                    _capture.Add(_scanner.Append(buffer, offset, read));
                    return;
                }

                _capture.Add(_scanner.Flush());
            }
        }
    }
}
