using System.Collections.Generic;

namespace FreeAIr
{
    /// <summary>
    /// Which MCP tools the user has approved for auto-execution without a per-call confirmation
    /// prompt - persisted through <see cref="InternalPage"/> so the approval survives across VS
    /// sessions instead of asking again every time.
    /// </summary>
    public sealed class MCPToolsExecutionStatus
    {
        /// <summary>Whether every tool, present and future, runs without confirmation - the "allow all" escape hatch.</summary>
        public bool EnabledAllTools
        {
            get;
            set;
        }

        /// <summary>The individually approved tool names, checked only when <see cref="EnabledAllTools"/> is false.</summary>
        public List<string> EnabledTools
        {
            get;
            set;
        }

        public MCPToolsExecutionStatus()
        {
            EnabledTools = new();
        }

        /// <summary>Whether <paramref name="toolName"/> may run without asking the user first.</summary>
        public bool IsToolEnabled(string toolName)
        {
            if (EnabledAllTools)
            {
                return true;
            }

            return EnabledTools.Contains(toolName);
        }

        /// <summary>Grants every tool auto-execution and persists it.</summary>
        public void EnableAllTools()
        {
            EnabledAllTools = true;
            Save();
        }

        /// <summary>Grants one tool auto-execution and persists it.</summary>
        public void EnableTool(string toolName)
        {
            EnabledTools.Add(toolName);
            Save();
        }

        /// <summary>Revokes every approval, back to asking for confirmation on every tool call.</summary>
        public void Reset()
        {
            EnabledAllTools = false;
            EnabledTools = new List<string>();
            Save();
        }

        private void Save()
        {
            InternalPage.Instance.MCPToolsExecutionStatus = System.Text.Json.JsonSerializer.Serialize(this);
            InternalPage.Instance.Save();
        }
    }
}
