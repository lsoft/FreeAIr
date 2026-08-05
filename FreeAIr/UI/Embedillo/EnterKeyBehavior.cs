using ICSharpCode.AvalonEdit;
using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;

namespace FreeAIr.UI.Embedillo
{
    /// <summary>
    /// XAML behavior attached to the chat prompt <see cref="TextEditor"/> that submits the message when the
    /// user presses Ctrl+Enter, instead of inserting a newline, by parsing the editor content and invoking
    /// the bound <see cref="Command"/>.
    /// </summary>
    public class EnterKeyBehavior : Behavior<TextEditor>
    {
        /// <summary>
        /// Backing dependency property for <see cref="Command"/>, the command executed on Ctrl+Enter.
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(EnterKeyBehavior));

        /// <summary>
        /// The command invoked with the parsed prompt content when the user presses Ctrl+Enter.
        /// </summary>
        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        /// <summary>
        /// Subscribes to the editor's key events once this behavior is attached.
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.KeyDown += OnKeyDown;
        }

        /// <summary>
        /// Unsubscribes from the editor's key events when this behavior is removed.
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.KeyDown -= OnKeyDown;
        }

        /// <summary>
        /// The <see cref="EmbedilloControl"/> hosting the prompt editor, used to parse the composed message
        /// (mentions, attachments, etc.) before it is submitted.
        /// </summary>
        public UIElement ParentControl
        {
            get;
            set;
        }

        /// <summary>
        /// Handles Ctrl+Enter in the prompt editor by parsing its content, executing <see cref="Command"/>
        /// with the parsed result, and clearing the editor; suppresses the default Enter behavior (inserting
        /// a newline) whenever Ctrl+Enter is pressed.
        /// </summary>
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
                )
            {
                if (ParentControl is EmbedilloControl parentControl)
                {
                    var parsed = parentControl.Parse();
                    if (parsed is not null)
                    {
                        if (
                            Command?.CanExecute(parsed) == true
                            )
                        {
                            Command.Execute(parsed);

                            textEditor.Clear();
                        }
                    }
                }
                
                e.Handled = true; // Предотвратить стандартное поведение Enter
            }
        }
    }
}
