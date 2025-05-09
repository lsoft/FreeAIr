using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit;
using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FreeAIr.UI.Embedillo
{
    public class EnterKeyBehavior : Behavior<TextEditor>
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(EnterKeyBehavior));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.KeyDown += OnKeyDown;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.KeyDown -= OnKeyDown;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            var textEditor = sender as TextEditor;
            if (textEditor is null)
            {
                return;
            }

            var text = textEditor.Text;

            if (
                e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control)
                && e.Key == Key.Enter
                && Command?.CanExecute(text) == true
                )
            {
                Command.Execute(text);

                textEditor.Clear();

                e.Handled = true; // Предотвратить стандартное поведение Enter
            }
        }
    }
}
