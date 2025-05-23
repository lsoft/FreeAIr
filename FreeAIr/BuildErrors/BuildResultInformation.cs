namespace FreeAIr.BuildErrors
{
    public sealed class BuildResultInformation
    {
        public ErrorInformationTypeEnum Type
        {
            get;
        }
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

        public BuildResultInformation(
            ErrorInformationTypeEnum type,
            string filePath,
            string errorDescription,
            int line,
            int column
            )
        {
            Type = type;
            FilePath = filePath;
            ErrorDescription = errorDescription;
            Line = line;
            Column = column;
        }

    }

    [Flags]
    public enum ErrorInformationTypeEnum
    {
        Error = 1,
        Warning = 2,
        Information = 4
    }
}
