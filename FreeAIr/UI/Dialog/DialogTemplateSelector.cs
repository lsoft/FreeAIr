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

        public DataTemplate DefaultToolCallContentTemplate
        {
            get;
            set;
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is not DialogContent dc)
            {
                return base.SelectTemplate(item, container);
            }

            var propertyName = dc.TemplatePropertyName;
            return (DataTemplate)typeof(DialogTemplateSelector).GetProperty(propertyName).GetValue(this);
        }
    }
}