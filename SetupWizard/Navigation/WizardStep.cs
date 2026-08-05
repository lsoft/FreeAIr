namespace FreeAIr.SetupWizard.Navigation
{
    /// <summary>One page of the setup wizard.</summary>
    public enum WizardStep
    {
        /// <summary>What the wizard is and what it is about to overwrite.</summary>
        Welcome,

        /// <summary>Built-in defaults / blank / current config - skipped on a first run, see <see cref="WizardNavigator"/>.</summary>
        StartingPoint,

        Agents,

        McpServers,

        Actions,

        MiscSettings,

        /// <summary>Read-only recap before writing the config.</summary>
        Summary,
    }
}
