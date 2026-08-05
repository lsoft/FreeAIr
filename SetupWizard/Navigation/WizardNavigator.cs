using System;
using System.Collections.Generic;

namespace FreeAIr.SetupWizard.Navigation
{
    /// <summary>
    /// Tracks which wizard step is current and what the visible step sequence is. On a first run
    /// there is no existing config worth asking "overwrite it how?" about, so <see cref="WizardStep.StartingPoint"/>
    /// is left out of the sequence entirely and the caller is expected to default to
    /// <c>WizardStartingPoint.BuiltInDefaults</c>. Cancelling is not modeled here - it is valid from
    /// every step and needs no navigator support.
    /// </summary>
    public sealed class WizardNavigator
    {
        private readonly List<WizardStep> _steps;
        private int _index;

        public bool IsFirstRun { get; }

        public WizardNavigator(bool isFirstRun)
        {
            IsFirstRun = isFirstRun;
            _steps = BuildSteps(isFirstRun);
            _index = 0;
        }

        /// <summary>The step sequence for a first run (no <see cref="WizardStep.StartingPoint"/>) or a later run (with it).</summary>
        public static List<WizardStep> BuildSteps(bool isFirstRun)
        {
            var steps = new List<WizardStep> { WizardStep.Welcome };

            if (!isFirstRun)
            {
                steps.Add(WizardStep.StartingPoint);
            }

            steps.Add(WizardStep.Agents);
            steps.Add(WizardStep.McpServers);
            steps.Add(WizardStep.Actions);
            steps.Add(WizardStep.MiscSettings);
            steps.Add(WizardStep.Summary);

            return steps;
        }

        public IReadOnlyList<WizardStep> Steps => _steps;

        public WizardStep CurrentStep => _steps[_index];

        public int CurrentIndex => _index;

        public bool CanGoBack => _index > 0;

        public bool CanGoNext => _index < _steps.Count - 1;

        public bool IsLastStep => _index == _steps.Count - 1;

        public WizardStep Next()
        {
            if (!CanGoNext)
            {
                throw new InvalidOperationException("Already at the last wizard step.");
            }

            _index++;
            return CurrentStep;
        }

        public WizardStep Back()
        {
            if (!CanGoBack)
            {
                throw new InvalidOperationException("Already at the first wizard step.");
            }

            _index--;
            return CurrentStep;
        }
    }
}
