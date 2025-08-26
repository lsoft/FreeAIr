using Microsoft.VisualStudio.Imaging;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace FreeAIr.Options2.Support
{
    public sealed class SupportCollectionJson : ICloneable
    {
        public List<SupportActionJson> Actions
        {
            get;
            set;
        }

        public SupportCollectionJson()
        {
            Actions = GetDefaultActions();
        }

        public object Clone()
        {
            return new SupportCollectionJson
            {
                Actions = Actions.ConvertAll(e => (SupportActionJson)e.Clone())
            };
        }

        private static List<SupportActionJson> GetDefaultActions() =>
            [
                new SupportActionJson
                {
                    Scopes = [ SupportScopeEnum.EnterPromptControl ],
                    Name = "Explain code",
                    AgentName = null,
                    Prompt = $@"Explain the code in the file(s):",
                    KnownMoniker = nameof(KnownMonikers.SQLServerObjectExplorer)
                },
                new SupportActionJson
                {
                    Scopes = [ SupportScopeEnum.EnterPromptControl ],
                    Name = "Add XML comments",
                    AgentName = null,
                    Prompt = $@"Add XML comments that match the code in the file(s):",
                    KnownMoniker = nameof(KnownMonikers.CodeReviewWizard)
                },
                new SupportActionJson
                {
                    Scopes = [ SupportScopeEnum.FileInSolutionTree ],
                    Name = "Generate unit tests",
                    AgentName = null,
                    Prompt = $@"I am using XUnit test framework. You need to generate a set of unit tests. Provide only one code snippet in your answer, without any additional information. Add XML comments for each test that describe what the test checks. Here are the file(s) you need to generate unit tests for: ",
                    KnownMoniker = nameof(KnownMonikers.TestGroup)
                },

                new SupportActionJson
                {
                    Scopes = [ SupportScopeEnum.SelectedCodeInDocument, SupportScopeEnum.CodelensInDocument, SupportScopeEnum.FileInSolutionTree ],
                    Name = "Explain code",
                    AgentName = null,
                    Prompt = $@"Explain the code in the file: {SupportContextVariableEnum.ContextItemName.GetAnchor()}.",
                    KnownMoniker = nameof(KnownMonikers.SQLServerObjectExplorer)
                },
                new SupportActionJson
                {
                    Scopes = [ SupportScopeEnum.SelectedCodeInDocument, SupportScopeEnum.CodelensInDocument, SupportScopeEnum.FileInSolutionTree ],
                    Name = "Add XML comments",
                    AgentName = null,
                    Prompt = $@"Add XML comments that match the code in the file {SupportContextVariableEnum.ContextItemName.GetAnchor()}. Do not shorten the source code.",
                    KnownMoniker = nameof(KnownMonikers.CodeReviewWizard)
                },
                new SupportActionJson
                {
                    Scopes = [ SupportScopeEnum.SelectedCodeInDocument, SupportScopeEnum.CodelensInDocument ],
                    Name = "Complete the code according to the comments",
                    AgentName = null,
                    Prompt = $@"Complete the code in the file {SupportContextVariableEnum.ContextItemName.GetAnchor()} according its comments.",
                    KnownMoniker = null
                },
                new SupportActionJson
                {
                    Scopes = [ SupportScopeEnum.SelectedCodeInDocument, SupportScopeEnum.CodelensInDocument, SupportScopeEnum.FileInSolutionTree ],
                    Name = "Generate unit tests",
                    AgentName = null,
                    Prompt = $@"Generate a set of unit tests for the code in the file {SupportContextVariableEnum.ContextItemName.GetAnchor()}. Provide only one code snippet in your answer, without any additional information. Add XML comments for each test that describe what the test checks. Write code for the {SupportContextVariableEnum.UnitTestFramework.GetAnchor()} test framework.",
                    KnownMoniker = nameof(KnownMonikers.TestGroup)
                },

                new SupportActionJson
                {
                    Scopes = [ SupportScopeEnum.BuildErrorWindow ],
                    Name = "Fix build error",
                    AgentName = null,
                    Prompt = $@"The compiler reported an error {SupportContextVariableEnum.BuildErrorMessage.GetAnchor()} in the file {SupportContextVariableEnum.ContextItemName.GetAnchor()}, line {SupportContextVariableEnum.BuildErrorLine.GetAnchor()}, column {SupportContextVariableEnum.BuildErrorColumn.GetAnchor()}. Help fix the code.",
                    KnownMoniker = null
                },

                new SupportActionJson
                {
                    Scopes = [ SupportScopeEnum.CommitMessageBuilding ],
                    Name = "Build commit message",
                    AgentName = null,
                    Prompt = $@"Generate commit message according the following git patch. Give in your reply only the commit message without additional information. Do not wrap the whole answer in any quotes.{Environment.NewLine}{Environment.NewLine}{SupportContextVariableEnum.GitDiff.GetAnchor()}",
                    KnownMoniker = nameof(KnownMonikers.GitRepository)
                },

                new SupportActionJson
                {
                    Scopes = [ SupportScopeEnum.NaturalLanguageSearch ],
                    Name = "Search by natural language",
                    AgentName = null,
                    Prompt = $@"Using the search query given to you, carefully examine all the attached files and find all the places that match the query. Your response should be a list in the following JSON format: `{{ ""matches"": [ {{""fullpath"": ""full path to the file"", ""found_text"":""a matched word sequence from document"", ""confidence_level"":a_level_of_your_confidence, ""line"":line_number_of_found_text, ""reason"":""a reason why you include this item to your result""}} ] }}`. `found_text` - 5-10 words from the document that you think match the search query, `confidence_level` is an integer, the level of your confidence that `found_text` matches the search query, where 0 is the minimum confidence, 100 is the maximum confidence, `line_number` is the line number where `found_text` was found.{Environment.NewLine}Search query:{SupportContextVariableEnum.NaturalLanguageSearchQuery.GetAnchor()}",
                    KnownMoniker = nameof(KnownMonikers.Search)
                },

                new SupportActionJson
                {
                    Scopes = [ SupportScopeEnum.BuildNaturalLanguageOutlines ],
                    Name = "Build natural language outlines",
                    AgentName = null,
                    Prompt = $@"Please provide summary description for the file: {SupportContextVariableEnum.ContextItemName.GetAnchor()}. Take into account existing comments. Provide only description, without adding code snippets or anything else.",
                    KnownMoniker = nameof(KnownMonikers.SetLanguage)
                },

                new SupportActionJson
                {
                    Scopes = [ SupportScopeEnum.GenerateNaturalLanguageOutlines ],
                    Name = "Generate and add natural language outlines",
                    AgentName = null,
                    Prompt = $@"Identify the logical sections of the code inside the files: {SupportContextVariableEnum.ContextItemName.GetAnchor()} and summarize these sections by generating comments.",
                    KnownMoniker = nameof(KnownMonikers.SetLanguage)
                },

                new SupportActionJson
                {
                    Scopes = [ SupportScopeEnum.WholeLineCompletion ],
                    Name = "Generate whole line completion",
                    AgentName = "agent_name_must_be_set",
                    Prompt = $@"In the document {SupportContextVariableEnum.ContextItemName.GetAnchor()} suggest whole local code completion where {SupportContextVariableEnum.WholeLineCompletionAnchor.GetAnchor()} anchor is set. Do not post whole modified document. Do not post anything except the code snipped you suggest to add to that place.",
                    KnownMoniker = nameof(KnownMonikers.CompletionMode)
                },

            ];

    }

    [JsonConverter(typeof(JsonDescriptionCommentConverter<SupportActionJson>))]
    public sealed class SupportActionJson : ICloneable
    {
        public HashSet<SupportScopeEnum> Scopes
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public string? AgentName
        {
            get;
            set;
        }

        public string Prompt
        {
            get;
            set;
        }

        [Description("Install KnownMonikers Explorer Visual Studio extension to choose correct image moniker.")]
        public string? KnownMoniker
        {
            get;
            set;
        }

        public SupportActionJson()
        {
            Scopes = new();
            Name = string.Empty;
            AgentName = null;
            Prompt = string.Empty;
            KnownMoniker = null;
        }

        public object Clone()
        {
            return new SupportActionJson
            {
                Scopes = new(Scopes),
                Name = Name,
                AgentName = AgentName,
                Prompt = Prompt,
                KnownMoniker = KnownMoniker
            };
        }
    }
}
