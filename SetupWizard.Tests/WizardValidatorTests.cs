using FreeAIr.SetupWizard.Validation;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FreeAIr.SetupWizard.Tests
{
    public class WizardValidatorTests
    {
        [Fact]
        public void ValidateAgentFields_ValidAgent_HasNoProblems()
        {
            var problems = WizardValidator.ValidateAgentFields("Local model", "http://localhost:5001/v1", "", 8192);

            Assert.Empty(problems);
        }

        [Fact]
        public void ValidateAgentFields_EmptyName_IsReported()
        {
            var problems = WizardValidator.ValidateAgentFields("", "http://localhost:5001/v1", "", 8192);

            Assert.Contains(AgentFieldProblem.MissingName, problems);
        }

        [Theory]
        [InlineData("not a uri")]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateAgentFields_InvalidEndpoint_IsReported(string endpoint)
        {
            var problems = WizardValidator.ValidateAgentFields("Agent", endpoint, "", 8192);

            Assert.Contains(AgentFieldProblem.InvalidEndpoint, problems);
        }

        [Fact]
        public void ValidateAgentFields_ZeroContextSize_IsReported()
        {
            var problems = WizardValidator.ValidateAgentFields("Agent", "http://localhost:5001/v1", "", 0);

            Assert.Contains(AgentFieldProblem.NonPositiveContextSize, problems);
        }

        [Fact]
        public void ValidateAgentFields_MalformedTokenForm_IsReported()
        {
            var problems = WizardValidator.ValidateAgentFields("Agent", "http://localhost:5001/v1", "{$}", 8192);

            Assert.Contains(AgentFieldProblem.InvalidTokenForm, problems);
        }

        [Theory]
        [InlineData("")]
        [InlineData("a-literal-token")]
        [InlineData("{$MY_TOKEN}")]
        public void IsValidTokenForm_AcceptsEmptyLiteralAndEnvReference(string token)
        {
            Assert.True(WizardValidator.IsValidTokenForm(token));
        }

        [Fact]
        public void IsValidTokenForm_RejectsEmptyEnvReference()
        {
            Assert.False(WizardValidator.IsValidTokenForm("{$}"));
        }

        [Fact]
        public void IsValidEndpoint_RejectsBlank()
        {
            Assert.False(WizardValidator.IsValidEndpoint(""));
            Assert.False(WizardValidator.IsValidEndpoint("   "));
        }

        [Fact]
        public void IsValidEndpoint_AcceptsAbsoluteUri()
        {
            Assert.True(WizardValidator.IsValidEndpoint("http://localhost:1234/v1"));
        }

        private static IEnumerable<ActionBinding> Actions(params (string Action, string? Agent)[] pairs)
        {
            return pairs.Select(p => new ActionBinding(p.Action, p.Agent));
        }

        [Fact]
        public void ValidateActionAgentBindings_EveryActionBoundToAKnownAgent_HasNoProblems()
        {
            var problems = WizardValidator.ValidateActionAgentBindings(
                Actions(("Explain code", "Local"), ("Generate unit tests", "Local")),
                new[] { "Local", "Cloud" }
                );

            Assert.Empty(problems);
        }

        [Fact]
        public void ValidateActionAgentBindings_ActionWithoutAnAgent_IsReported()
        {
            var problems = WizardValidator.ValidateActionAgentBindings(
                Actions(("Explain code", null)),
                new[] { "Local" }
                );

            var problem = Assert.Single(problems);
            Assert.Equal("Explain code", problem.ActionName);
            Assert.Equal(ActionBindingProblemKind.NoAgentAssigned, problem.Kind);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateActionAgentBindings_BlankAgentNameCountsAsUnassigned(string agentName)
        {
            var problems = WizardValidator.ValidateActionAgentBindings(
                Actions(("Explain code", agentName)),
                new[] { "Local" }
                );

            Assert.Equal(ActionBindingProblemKind.NoAgentAssigned, Assert.Single(problems).Kind);
        }

        [Fact]
        public void ValidateActionAgentBindings_ActionNamingADeletedAgent_IsReported()
        {
            var problems = WizardValidator.ValidateActionAgentBindings(
                Actions(("Fix build error", "Renamed away")),
                new[] { "Local" }
                );

            var problem = Assert.Single(problems);
            Assert.Equal("Renamed away", problem.AgentName);
            Assert.Equal(ActionBindingProblemKind.UnknownAgent, problem.Kind);
        }

        [Fact]
        public void ValidateActionAgentBindings_AgentNamesAreCaseSensitive()
        {
            var problems = WizardValidator.ValidateActionAgentBindings(
                Actions(("Explain code", "local")),
                new[] { "Local" }
                );

            Assert.Equal(ActionBindingProblemKind.UnknownAgent, Assert.Single(problems).Kind);
        }

        [Fact]
        public void ValidateActionAgentBindings_NoAgentsAtAll_ReportsEveryAction()
        {
            var problems = WizardValidator.ValidateActionAgentBindings(
                Actions(("Explain code", "Local"), ("Fix build error", null)),
                new string[0]
                );

            Assert.Equal(2, problems.Count);
        }

        [Fact]
        public void ValidateActionAgentBindings_NoActions_HasNoProblems()
        {
            var problems = WizardValidator.ValidateActionAgentBindings(
                Actions(),
                new[] { "Local" }
                );

            Assert.Empty(problems);
        }

        [Fact]
        public void ValidateActionAgentBindings_UnboundWholeLineCompletion_IsIgnoredWhileTheFeatureIsOff()
        {
            var problems = WizardValidator.ValidateActionAgentBindings(
                new[] { new ActionBinding("Complete the whole line", null, isWholeLineCompletion: true) },
                new[] { "Local" },
                wholeLineCompletionEnabled: false
                );

            Assert.Empty(problems);
        }

        [Fact]
        public void ValidateActionAgentBindings_UnboundWholeLineCompletion_IsReportedOnceTheFeatureIsOn()
        {
            var problems = WizardValidator.ValidateActionAgentBindings(
                new[] { new ActionBinding("Complete the whole line", null, isWholeLineCompletion: true) },
                new[] { "Local" },
                wholeLineCompletionEnabled: true
                );

            Assert.Equal(ActionBindingProblemKind.NoAgentAssigned, Assert.Single(problems).Kind);
        }

        [Fact]
        public void ValidateActionAgentBindings_WholeLineCompletionNamingADeletedAgent_IsReportedOnceTheFeatureIsOn()
        {
            var problems = WizardValidator.ValidateActionAgentBindings(
                new[] { new ActionBinding("Complete the whole line", "agent_name_must_be_set", isWholeLineCompletion: true) },
                new[] { "Local" },
                wholeLineCompletionEnabled: true
                );

            Assert.Equal(ActionBindingProblemKind.UnknownAgent, Assert.Single(problems).Kind);
        }

        [Fact]
        public void ValidateActionAgentBindings_TheWholeLineSwitchDoesNotSilenceTheOtherActions()
        {
            var problems = WizardValidator.ValidateActionAgentBindings(
                new[]
                {
                    new ActionBinding("Complete the whole line", null, isWholeLineCompletion: true),
                    new ActionBinding("Explain code", null),
                },
                new[] { "Local" },
                wholeLineCompletionEnabled: false
                );

            Assert.Equal("Explain code", Assert.Single(problems).ActionName);
        }
    }
}
