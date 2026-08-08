using System;
using System.Collections.Generic;
using FreeAIr.SetupWizard.Helper;

namespace FreeAIr.SetupWizard.Validation
{
    /// <summary>
    /// What is wrong with one field of the agent being edited on the wizard's agent step. Reported
    /// as a code rather than as a sentence because this project carries no resources - the WPF
    /// layer turns each value into the localized message the user actually reads.
    /// </summary>
    public enum AgentFieldProblem
    {
        MissingName,
        InvalidEndpoint,
        InvalidTokenForm,
        NonPositiveContextSize,
    }

    /// <summary>Why one action cannot run as configured: see <see cref="ActionBindingProblem"/>.</summary>
    public enum ActionBindingProblemKind
    {
        /// <summary>The action names no agent at all, so invoking it will ask the user to pick one every time.</summary>
        NoAgentAssigned,

        /// <summary>The action names an agent which is not in the configuration - typically one that was renamed or deleted.</summary>
        UnknownAgent,
    }

    /// <summary>
    /// One action as the wizard hands it over to be checked: the name the user sees, the agent it
    /// asks for, and whether it is the whole line completion action.
    ///
    /// That last flag exists because whole line completion is the one action with a switch of its
    /// own. While the switch is off the action never runs, so a missing agent on it is not a
    /// problem worth a red warning - see <see cref="WizardValidator.ValidateActionAgentBindings"/>.
    /// </summary>
    public sealed class ActionBinding
    {
        public string ActionName
        {
            get;
        }

        public string? AgentName
        {
            get;
        }

        public bool IsWholeLineCompletion
        {
            get;
        }

        public ActionBinding(string actionName, string? agentName, bool isWholeLineCompletion = false)
        {
            ActionName = actionName;
            AgentName = agentName;
            IsWholeLineCompletion = isWholeLineCompletion;
        }
    }

    /// <summary>One action whose agent binding is broken, paired with the reason, for the red warning on the actions step.</summary>
    public sealed class ActionBindingProblem
    {
        public string ActionName
        {
            get;
        }

        public string? AgentName
        {
            get;
        }

        public ActionBindingProblemKind Kind
        {
            get;
        }

        public ActionBindingProblem(string actionName, string? agentName, ActionBindingProblemKind kind)
        {
            ActionName = actionName;
            AgentName = agentName;
            Kind = kind;
        }
    }

    /// <summary>
    /// The checks behind the wizard's two hand-written editing steps: the agent fields it collects
    /// itself, and whether the actions carried over from the shipped defaults or from an existing
    /// configuration still point at an agent that exists. Both work on plain values rather than on
    /// <c>AgentJson</c>/<c>SupportActionJson</c>, so this project needs no DTO mirroring them.
    /// </summary>
    public static class WizardValidator
    {
        public static IReadOnlyList<AgentFieldProblem> ValidateAgentFields(string name, string endpoint, string token, int contextSize)
        {
            var problems = new List<AgentFieldProblem>();

            if (string.IsNullOrWhiteSpace(name))
            {
                problems.Add(AgentFieldProblem.MissingName);
            }

            if (!IsValidEndpoint(endpoint))
            {
                problems.Add(AgentFieldProblem.InvalidEndpoint);
            }

            if (!IsValidTokenForm(token))
            {
                problems.Add(AgentFieldProblem.InvalidTokenForm);
            }

            if (contextSize <= 0)
            {
                problems.Add(AgentFieldProblem.NonPositiveContextSize);
            }

            return problems;
        }

        /// <summary>
        /// Cross-checks every action's agent name against the agents that actually exist. This is
        /// what makes editing an agent visible on the actions step: renaming or deleting an agent
        /// silently strands every action that named it, and an action pointing at nothing fails
        /// only when the user finally invokes it.
        ///
        /// The whole line completion action is the exception: it is checked only while the feature
        /// is switched on, because with the switch off nothing ever invokes it and complaining
        /// about a feature the user did not ask for is noise.
        /// </summary>
        /// <param name="actions">The actions of the configuration being built.</param>
        /// <param name="agentNames">The names of the agents in the configuration being built.</param>
        /// <param name="wholeLineCompletionEnabled">The state of the `complete the whole line as you type` switch.</param>
        public static IReadOnlyList<ActionBindingProblem> ValidateActionAgentBindings(
            IEnumerable<ActionBinding> actions,
            IEnumerable<string> agentNames,
            bool wholeLineCompletionEnabled = false
            )
        {
            if (actions is null)
            {
                throw new ArgumentNullException(nameof(actions));
            }

            if (agentNames is null)
            {
                throw new ArgumentNullException(nameof(agentNames));
            }

            var known = new HashSet<string>(StringComparer.Ordinal);
            foreach (var agentName in agentNames)
            {
                if (!string.IsNullOrWhiteSpace(agentName))
                {
                    known.Add(agentName);
                }
            }

            var problems = new List<ActionBindingProblem>();

            foreach (var action in actions)
            {
                if (action.IsWholeLineCompletion && !wholeLineCompletionEnabled)
                {
                    //dormant: the feature is off, so its agent binding does not matter yet
                    continue;
                }

                if (string.IsNullOrWhiteSpace(action.AgentName))
                {
                    problems.Add(new ActionBindingProblem(action.ActionName, action.AgentName, ActionBindingProblemKind.NoAgentAssigned));
                }
                else if (!known.Contains(action.AgentName!))
                {
                    problems.Add(new ActionBindingProblem(action.ActionName, action.AgentName, ActionBindingProblemKind.UnknownAgent));
                }
            }

            return problems;
        }

        public static bool IsValidEndpoint(string? endpoint)
        {
            return !string.IsNullOrWhiteSpace(endpoint) && Uri.TryCreate(endpoint, UriKind.Absolute, out _);
        }

        public static bool IsValidTokenForm(string? token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return true;
            }

            if (DirectOrEnvStringHelper.IsEnvReference(token!))
            {
                return DirectOrEnvStringHelper.TryGetVarName(token!, out var varName) && !string.IsNullOrWhiteSpace(varName);
            }

            return true;
        }
    }
}
