using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

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
                    Scopes = [ SupportScopeEnum.SelectedCodeInDocument, SupportScopeEnum.CodelensInDocument, SupportScopeEnum.FileInSolutionTree ],
                    Name = "Explain code",
                    AgentName = null,
                    Prompt = $@"Explain the code in the file: `{SupportContextVariableEnum.ContextItemFilePath.GetAnchor()}`."
                },
                new SupportActionJson
                {
                    Scopes = [ SupportScopeEnum.SelectedCodeInDocument, SupportScopeEnum.CodelensInDocument, SupportScopeEnum.FileInSolutionTree ],
                    Name = "Add XML comments",
                    AgentName = null,
                    Prompt = $@"Add XML comments that match the code in the file `{SupportContextVariableEnum.ContextItemFilePath.GetAnchor()}`. Do not shorten the source code."
                },
                new SupportActionJson
                {
                    Scopes = [ SupportScopeEnum.SelectedCodeInDocument, SupportScopeEnum.CodelensInDocument ],
                    Name = "Complete the code according to the comments",
                    AgentName = null,
                    Prompt = $@"Complete the code in the file `{SupportContextVariableEnum.ContextItemFilePath.GetAnchor()}` according its comments."
                },
                new SupportActionJson
                {
                    Scopes = [ SupportScopeEnum.SelectedCodeInDocument, SupportScopeEnum.CodelensInDocument, SupportScopeEnum.FileInSolutionTree ],
                    Name = "Generate unit tests",
                    AgentName = null,
                    Prompt = $@"Generate a set of unit tests for the code in the file `{SupportContextVariableEnum.ContextItemFilePath.GetAnchor()}`. Provide only one code snippet in your answer, without any additional information. Add XML comments for each test that describe what the test checks. Write code for the {SupportContextVariableEnum.UnitTestFramework.GetAnchor()} test framework."
                },

                new SupportActionJson
                {
                    Scopes = [ SupportScopeEnum.BuildErrorWindow ],
                    Name = "Fix build error",
                    AgentName = null,
                    Prompt = $@"The compiler reported an error `{SupportContextVariableEnum.BuildErrorMessage.GetAnchor()}` in the file `{SupportContextVariableEnum.ContextItemFilePath.GetAnchor()}`, line {SupportContextVariableEnum.BuildErrorLine.GetAnchor()}, column {SupportContextVariableEnum.BuildErrorColumn.GetAnchor()}. Help fix the code."
                },
               
            ];

    }

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

        public SupportActionJson()
        {
            Scopes = new();
            Name = string.Empty;
            AgentName = null;
            Prompt = string.Empty;
        }

        public object Clone()
        {
            return new SupportActionJson
            {
                Scopes = new(Scopes),
                Name = Name,
                AgentName = AgentName,
                Prompt = Prompt
            };
        }
    }

    public enum SupportScopeEnum
    {
        SelectedCodeInDocument,
        CodelensInDocument,
        FileInSolutionTree,
        BuildErrorWindow
    }
}
