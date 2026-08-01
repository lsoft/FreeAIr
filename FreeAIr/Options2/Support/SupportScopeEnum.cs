namespace FreeAIr.Options2.Support
{
    /// <summary>
    /// The user gestures a support action can be offered for. An action declares the scopes it
    /// belongs to in the json settings, and FreeAIr shows only the actions whose scopes match
    /// what the user has just done.
    ///
    /// These names are written into the settings file as strings, so renaming a member breaks
    /// the settings of everyone who already uses it.
    /// </summary>
    public enum SupportScopeEnum
    {
        /// <summary>A piece of code is selected in the editor.</summary>
        SelectedCodeInDocument,

        /// <summary>The FreeAIr codelens above a member is clicked.</summary>
        CodelensInDocument,

        /// <summary>Files are selected in Solution Explorer.</summary>
        FileInSolutionTree,

        /// <summary>An error is selected in the Error List.</summary>
        BuildErrorWindow,

        /// <summary>`/` is typed in the chat prompt area.</summary>
        EnterPromptControl,

        /// <summary>A commit message is being composed in the `Git Changes` window.</summary>
        CommitMessageBuilding,

        /// <summary>A natural language search over the solution or the project is started.</summary>
        NaturalLanguageSearch,

        /// <summary>NLO embedding json files are being built.</summary>
        BuildNaturalLanguageOutlines,

        /// <summary>Natural language outlines are being generated for source files.</summary>
        GenerateNaturalLanguageOutlines,

        /// <summary>A whole line completion is requested at the caret.</summary>
        WholeLineCompletion,

        /// <summary>A dictated prompt has been transcribed and is about to be cleaned up.</summary>
        RecordPostProcess
    }
}
