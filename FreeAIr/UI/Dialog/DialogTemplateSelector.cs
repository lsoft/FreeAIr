using System.Windows;
using System.Windows.Controls;

namespace FreeAIr.UI.Dialog.Content
{
    public class DialogTemplateSelector : DataTemplateSelector
    {
        public DataTemplate PromptContentTemplate
        {
            get;
            set;
        }

        public DataTemplate AnswerContentTemplate
        {
            get;
            set;
        }

        public DataTemplate ToolCallContentTemplate
        {
            get;
            set;
        }

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