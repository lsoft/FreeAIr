using System.Windows;
using System.Windows.Controls;

namespace FreeAIr.UI.Dialog.Content
{
    /// <summary>Chooses the WPF template for a chat bubble based on the concrete <see cref="DialogContent"/> type (prompt, answer or tool call).</summary>
    public class DialogTemplateSelector : DataTemplateSelector
    {
        /// <summary>Template used for user prompt bubbles.</summary>
        public DataTemplate PromptContentTemplate
        {
            get;
            set;
        }

        /// <summary>Template used for LLM answer bubbles.</summary>
        public DataTemplate AnswerContentTemplate
        {
            get;
            set;
        }

        /// <summary>Template used for tool-call bubbles.</summary>
        public DataTemplate ToolCallContentTemplate
        {
            get;
            set;
        }

        /// <summary>Picks the matching template based on the bound item's <see cref="DialogContent"/> subtype.</summary>
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is PromptDialogContent)
                return PromptContentTemplate;
            if (item is AnswerDialogContent)
                return AnswerContentTemplate;
            else if (item is ToolCallDialogContent)
                return ToolCallContentTemplate;

            return base.SelectTemplate(item, container);
        }
    }
}