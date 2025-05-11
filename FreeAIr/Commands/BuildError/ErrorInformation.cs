namespace FreeAIr.Commands.BuildError
{
    public sealed class ErrorInformation
    {
        public string FilePath
        {
            get;
        }
        public string ErrorDescription
        {
            get;
        }
        public int Line
        {
            get;
        }
        public int Column
        {
            get;
        }

        public ErrorInformation(
            string filePath,
            string errorDescription,
            int line,
            int column
            )
        {
            FilePath = filePath;
            ErrorDescription = errorDescription;
            Line = line;
            Column = column;
        }

    }
}
