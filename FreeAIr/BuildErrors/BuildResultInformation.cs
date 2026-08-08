namespace FreeAIr.BuildErrors
{
    /// <summary>
    /// A single entry copied out of the Visual Studio Error List: one build error, warning or
    /// informational message with the file, line and column it points to.
    /// </summary>
    public sealed class BuildResultInformation
    {
        /// <summary>
        /// Whether this entry is an error, a warning or an informational message.
        /// </summary>
        public ErrorInformationTypeEnum Type
        {
            get;
        }
        /// <summary>
        /// Path to the source file the Error List entry refers to.
        /// </summary>
        public string FilePath
        {
            get;
        }
        /// <summary>
        /// The compiler/build message text as shown in the Error List.
        /// </summary>
        public string ErrorDescription
        {
            get;
        }
        /// <summary>
        /// One-based line number in <see cref="FilePath"/> the entry points to.
        /// </summary>
        public int Line
        {
            get;
        }
        /// <summary>
        /// One-based column number in <see cref="FilePath"/> the entry points to.
        /// </summary>
        public int Column
        {
            get;
        }

        /// <summary>
        /// Builds an Error List entry snapshot from the values read off an <c>IVsTaskItem</c>.
        /// </summary>
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

    /// <summary>
    /// Severity of an Error List entry; used both to classify a <see cref="BuildResultInformation"/>
    /// and as a filter mask when querying the list.
    /// </summary>
    [Flags]
    public enum ErrorInformationTypeEnum
    {
        /// <summary>A build error entry.</summary>
        Error = 1,
        /// <summary>A build warning entry.</summary>
        Warning = 2,
        /// <summary>An informational (non-error, non-warning) entry.</summary>
        Information = 4
    }
}
