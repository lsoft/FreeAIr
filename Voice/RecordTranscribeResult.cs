namespace FreeAIr.Record
{
    /// <summary>
    /// The outcome of <see cref="IRecorder.RecordAndTranscribeAsync"/>: either the transcribed text
    /// or an error message, never both. Callers use <see cref="TryGetText"/> /
    /// <see cref="TryGetError"/> rather than branching on <see cref="IsSuccess"/> directly.
    /// </summary>
    public sealed class RecordTranscribeResult
    {
        private readonly string _text;

        /// <summary>Whether transcription produced text, as opposed to an error.</summary>
        public bool IsSuccess
        {
            get;
        }

        /// <summary>Wraps either the transcribed text or the error message; use <see cref="FromSuccess"/>/<see cref="FromFailure(string)"/> instead of calling this directly.</summary>
        public RecordTranscribeResult(bool isSuccess, string text)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            IsSuccess = isSuccess;
            _text = text;
        }

        /// <summary>The transcribed text, when <see cref="IsSuccess"/> is true.</summary>
        public bool TryGetText(out string? text)
        {
            if (IsSuccess)
            {
                text = _text;
                return true;
            }

            text = null;
            return false;
        }

        /// <summary>The error message, when <see cref="IsSuccess"/> is false.</summary>
        public bool TryGetError(out string? error)
        {
            if (IsSuccess)
            {
                error = null;
                return false;
            }

            error = _text;
            return true;
        }

        /// <summary>A successful result carrying the transcribed text.</summary>
        public static RecordTranscribeResult FromSuccess(string text)
        {
            return new RecordTranscribeResult(true, text);
        }

        /// <summary>A failed result carrying an error message to show the user.</summary>
        public static RecordTranscribeResult FromFailure(string error)
        {
            return new RecordTranscribeResult(false, error);
        }

        /// <summary>A failed result built from an exception, carrying its message and stack trace.</summary>
        public static RecordTranscribeResult FromFailure(Exception excp)
        {
            return new RecordTranscribeResult(
                false,
                excp.Message
                + Environment.NewLine
                + excp.StackTrace
                );
        }
    }
}
