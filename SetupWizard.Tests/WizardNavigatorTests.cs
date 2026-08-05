using FreeAIr.SetupWizard.Navigation;
using Xunit;

namespace FreeAIr.SetupWizard.Tests
{
    public class WizardNavigatorTests
    {
        [Fact]
        public void FirstRun_SkipsStartingPointStep()
        {
            var navigator = new WizardNavigator(isFirstRun: true);

            Assert.DoesNotContain(WizardStep.StartingPoint, navigator.Steps);
        }

        [Fact]
        public void NotFirstRun_IncludesStartingPointStep()
        {
            var navigator = new WizardNavigator(isFirstRun: false);

            Assert.Contains(WizardStep.StartingPoint, navigator.Steps);
        }

        [Fact]
        public void FirstRun_StartsOnWelcomeAndEndsOnSummary()
        {
            var navigator = new WizardNavigator(isFirstRun: true);

            Assert.Equal(WizardStep.Welcome, navigator.CurrentStep);
            Assert.Equal(WizardStep.Summary, navigator.Steps[^1]);
        }

        [Fact]
        public void Next_AdvancesThroughEveryStepInOrder()
        {
            var navigator = new WizardNavigator(isFirstRun: false);
            var expected = WizardNavigator.BuildSteps(isFirstRun: false);

            var visited = new System.Collections.Generic.List<WizardStep> { navigator.CurrentStep };
            while (navigator.CanGoNext)
            {
                visited.Add(navigator.Next());
            }

            Assert.Equal(expected, visited);
            Assert.True(navigator.IsLastStep);
        }

        [Fact]
        public void Back_ReturnsToThePreviousStep()
        {
            var navigator = new WizardNavigator(isFirstRun: false);
            navigator.Next();
            navigator.Next();

            var previous = navigator.Back();

            Assert.Equal(WizardStep.StartingPoint, previous);
        }

        [Fact]
        public void Next_AtLastStep_Throws()
        {
            var navigator = new WizardNavigator(isFirstRun: true);
            while (navigator.CanGoNext)
            {
                navigator.Next();
            }

            Assert.Throws<System.InvalidOperationException>(() => navigator.Next());
        }

        [Fact]
        public void Back_AtFirstStep_Throws()
        {
            var navigator = new WizardNavigator(isFirstRun: true);

            Assert.Throws<System.InvalidOperationException>(() => navigator.Back());
        }
    }
}
