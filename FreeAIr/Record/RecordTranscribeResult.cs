namespace FreeAIr.Record
{
    public sealed class RecordTranscribeResult
    {
        private readonly string _text;

        public bool IsSuccess
        {
            get;
        }

        public RecordTranscribeResult(bool isSuccess, string text)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            IsSuccess = isSuccess;
            _text = text;
        }

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

        public static RecordTranscribeResult FromSuccess(string text)
        {
            return new RecordTranscribeResult(true, text);
        }

        public static RecordTranscribeResult FromFailure(string error)
        {
            return new RecordTranscribeResult(false, error);
        }

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
