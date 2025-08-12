using System.Windows;
using System.Windows.Controls;

namespace FreeAIr.UI.Dialog
{
    public class DialogTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ReplicContentTemplate
        {
            get; set;
        }

        public DataTemplate ToolCallContentTemplate
        {
            get; set;
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ReplicContent)
                return ReplicContentTemplate;
            else if (item is ToolCallContent)
                return ToolCallContentTemplate;

            return base.SelectTemplate(item, container);
        }
    }
}