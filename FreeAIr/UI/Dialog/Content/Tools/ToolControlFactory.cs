using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FreeAIr.UI.Dialog.Content.Tools
{
    public static class ToolControlFactory
    {
        public static object? CreateToolControl(
            StreamingChatToolCallUpdate toolCall
            )
        {
            switch (toolCall.FunctionName)
            {
                case "VisualStudio.ReplaceDocumentBody":
                    return VisualStudio_ReplaceDocumentBody_UserControl.Create(
                        toolCall
                        );
            }

            return null;
        }

    }


}
