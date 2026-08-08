using System.ComponentModel;

namespace FreeAIr
{
    /// <summary>
    /// Hidden options page used to persist FreeAIr's internal/diagnostic state between sessions —
    /// the last seen extension version and the serialized MCP tools execution status — rather than
    /// user-facing preferences.
    /// </summary>
    [Browsable(false)]
    public class InternalPage : BaseOptionModel<InternalPage>
    {

        /// <summary>
        /// Unused placeholder property; this options page intentionally has nothing to show the user.
        /// </summary>
        [Category("Internal options")]
        [DisplayName("Internal option")]
        [Description("This page is used to store an internal options of FreeAIr, so you see nothing here.")]
        [DefaultValue("")]
        public string Empty
        {
            get;
            set;
        } = string.Empty;

        /// <summary>
        /// Reserved storage slot for FreeAIr's internal options; not shown in the Options dialog.
        /// </summary>
        [Category("Internal options")]
        [DisplayName("All")]
        [Description("Options for FreeAIr")]
        [DefaultValue("")]
        [Browsable(false)]
        public string Options
        {
            get;
            set;
        }

        /// <summary>
        /// Version of FreeAIr that last ran, used to detect upgrades and trigger any needed
        /// one-time migration or "what's new" logic.
        /// </summary>
        [Category("Logic")]
        [DisplayName("FreeAIr Last Version")]
        [DefaultValue("2.0.0")]
        [Browsable(false)]
        public string FreeAIrLastVersion
        {
            get;
            set;
        }

        /// <summary>
        /// Whether the first-run setup wizard info bar has already been shown (or dismissed) once.
        /// False on a brand new install; used to tell "first run" apart from "upgraded from an
        /// older version" so the two info bars (setup wizard vs. release notes) don't both fire.
        /// </summary>
        [Category("Logic")]
        [DisplayName("Setup Wizard Introduced")]
        [DefaultValue(false)]
        [Browsable(false)]
        public bool SetupWizardIntroduced
        {
            get;
            set;
        }

        /// <summary>
        /// JSON-serialized <see cref="FreeAIr.MCPToolsExecutionStatus"/> tracking which MCP tools
        /// have been executed, persisted across sessions.
        /// </summary>
        [Category("Logic")]
        [DisplayName("MCPToolsExecutionStatus")]
        [DefaultValue("")]
        [Browsable(false)]
        public string MCPToolsExecutionStatus
        {
            get;
            set;
        }

        /// <summary>
        /// Clears the stored MCP tools execution status and saves the options page.
        /// </summary>
        public void ResetMCPToolsExecutionStatus()
        {
            MCPToolsExecutionStatus = string.Empty;
            Save();
        }

        /// <summary>
        /// Deserializes the stored MCP tools execution status, returning a fresh empty status if
        /// nothing is stored yet or the stored JSON cannot be parsed.
        /// </summary>
        public MCPToolsExecutionStatus ReadMCPToolsExecutionStatus()
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<MCPToolsExecutionStatus>(MCPToolsExecutionStatus);
            }
            catch
            {
                //suppress
            }

            return new MCPToolsExecutionStatus();
        }
    }
}
