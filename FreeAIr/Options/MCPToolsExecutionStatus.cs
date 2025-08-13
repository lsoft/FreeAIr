using System.Collections.Generic;

namespace FreeAIr
{
    public sealed class MCPToolsExecutionStatus
    {
        public bool EnabledAllTools
        {
            get;
            set;
        }

        public List<string> EnabledTools
        {
            get;
            set;
        }

        public MCPToolsExecutionStatus()
        {
            EnabledTools = new();
        }

        public bool IsToolEnabled(string toolName)
        {
            if (EnabledAllTools)
            {
                return true;
            }

            return EnabledTools.Contains(toolName);
        }

        public void EnableAllTools()
        {
            EnabledAllTools = true;
            Save();
        }

        public void EnableTool(string toolName)
        {
            EnabledTools.Add(toolName);
            Save();
        }

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
