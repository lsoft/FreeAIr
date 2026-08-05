using FreeAIr.Antlr;
using FreeAIr.Helper;
using FreeAIr.UI.Embedillo.Answer.Parser;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FreeAIr.UI.Embedillo
{
    /// <summary>
    /// The Embedillo chat input editor: an AvalonEdit-based text box that supports @-mention
    /// completion (files, commands, solution items) via <see cref="MentionVisualLineGenerator"/>
    /// and turns the typed text into a <see cref="Parsed"/> prompt through an <see cref="IParser"/>.
    /// </summary>
    public partial class EmbedilloControl : UserControl
    {
        /// <summary>
        /// The mention completion generators registered by <see cref="Setup(IParser)"/>, one per
        /// anchor symbol (e.g. '@' for files, '/' for commands), used to trigger completion popups
        /// as the user types.
        /// </summary>
        private readonly List<MentionVisualLineGenerator> _generators = new();
        /// <summary>
        /// The parser that turns the raw editor text into a <see cref="Parsed"/> prompt once the
        /// user submits; set by <see cref="Setup(IParser)"/>.
        /// </summary>
        private IParser? _parser;

        /// <summary>
        /// Dependency property backing <see cref="EnterCommand"/>, the command invoked when the
        /// user submits the Embedillo prompt (typically by pressing Enter).
        /// </summary>
        public static readonly DependencyProperty EnterCommandProperty =
            DependencyProperty.Register(
                nameof(EnterCommand),
                typeof(ICommand),
                typeof(EmbedilloControl));

        /// <summary>
        /// Dependency property backing <see cref="ControlEnabled"/>, which toggles whether the
        /// Embedillo input accepts text and shows its hint placeholder.
        /// </summary>
        public static readonly DependencyProperty ControlEnabledProperty =
            DependencyProperty.Register(
                nameof(ControlEnabled),
                typeof(bool),
                typeof(EmbedilloControl),
                new PropertyMetadata(default(bool), OnControlEnabledChanged)
                );

        /// <summary>
        /// Dependency property backing <see cref="MaxHeight"/>, the maximum height the Embedillo
        /// text editor may grow to before scrolling.
        /// </summary>
        public static readonly DependencyProperty MaxHeightProperty =
            DependencyProperty.Register(
                nameof(MaxHeight),
                typeof(int),
                typeof(EmbedilloControl),
                new PropertyMetadata(default(int), OnControlEnabledChanged)
                );

        /// <summary>
        /// The maximum height, in pixels, the underlying AvalonEdit text area is allowed to expand to.
        /// </summary>
        public int MaxHeight
        {
            get => (int)GetValue(MaxHeightProperty);
            set => SetValue(MaxHeightProperty, value);
        }

        /// <summary>
        /// Whether the Embedillo input is currently interactive; when false the hint placeholder
        /// is hidden and the control behaves as disabled.
        /// </summary>
        public bool ControlEnabled
        {
            get => (bool)GetValue(ControlEnabledProperty);
            set => SetValue(ControlEnabledProperty, value);
        }

        /// <summary>
        /// The placeholder text shown in the editor (e.g. "Ask a question...") while it is empty.
        /// </summary>
        public string HintText
        {
            get;
            set;
        }

        /// <summary>
        /// The command executed when the user submits the current prompt, typically bound to the
        /// chat view model's "send message" action.
        /// </summary>
        public ICommand EnterCommand
        {
            get => (ICommand)GetValue(EnterCommandProperty);
            set => SetValue(EnterCommandProperty, value);
        }

        /// <summary>
        /// Computes whether the hint placeholder should be visible: hidden when the control is
        /// disabled or the editor already contains text.
        /// </summary>
        public Visibility HintVisibility
        {
            get
            {
                if (!ControlEnabled)
                {
                    return Visibility.Hidden;
                }

                return string.IsNullOrEmpty(AvalonTextEditor.Text)
                    ? Visibility.Visible
                    : Visibility.Hidden
                    ;
            }
        }

        /// <summary>
        /// Wires up the AvalonEdit text area: mention-completion popups on trigger characters,
        /// hint visibility refresh on text changes, and custom handling of backspace/delete/ctrl
        /// combinations so multi-character mention tokens are deleted as a unit.
        /// </summary>
        public EmbedilloControl()
        {
            InitializeComponent();

            ControlEnabled = true;

            #region setup AvalonTextEditor

            AvalonTextEditor.TextArea.TextEntered += (object sender, TextCompositionEventArgs e) =>
            {
                var generator = _generators.FirstOrDefault(g => e.Text == g.AnchorSymbol.ToString());
                if (generator is not null)
                {
                    ShowCompletionWindowAsync(generator)
                        .FileAndForget(nameof(ShowCompletionWindowAsync))
                        ;
                }
            };

            AvalonTextEditor.TextChanged += (object sender, EventArgs e) =>
            {
                UpdateHintStatus();
            };

            AvalonTextEditor.KeyUp += (object sender, KeyEventArgs e) =>
            {
                UpdateHintStatus();
            };

            AvalonTextEditor.PreviewKeyDown += (object sender, KeyEventArgs e) =>
            {
                var ki = e.Key.IsTextChangedCombination(e.KeyboardDevice.Modifiers);
                if (!ki.IsTextChangedCombination || ki.EnteredText is null)
                {
                    return;
                }

                if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift) && e.Key == Key.Delete)
                {
                    //shift + delete не будем обрабатывать
                    //TODO: но неплохо бы сделать
                    e.Handled = true;
                    return;
                }
                if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.Z)
                {
                    //ctrl + Z не будем обрабатывать
                    //TODO: но неплохо бы сделать
                    e.Handled = true;
                    return;
                }

                var selectedText = AvalonTextEditor.SelectedText;

                //если действие пользователя связано с копированием В буфер обмена
                //сразу обработаем эту операцию
                ki.PostProcessCopyToClipboard(
                    selectedText
                    );

                if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.Back)
                {
                    //ctrl + backspace
                    ProcessLogic(
                        TextChangeModeEnum.CtrlBackspace,
                        ki.EnteredText
                        );
                    e.Handled = true; //блокируем стандартную обработку
                }
                if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.Delete)
                {
                    //ctrl + delete
                    ProcessLogic(
                        TextChangeModeEnum.CtrlDelete,
                        ki.EnteredText
                        );
                    e.Handled = true; //блокируем стандартную обработку
                }
                else if (e.Key == Key.Back)
                {
                    ProcessLogic(
                        TextChangeModeEnum.Backspace,
                        ki.EnteredText
                        );
                    e.Handled = true; //блокируем стандартную обработку
                }
                else if (e.Key == Key.Delete)
                {
                    ProcessLogic(
                        TextChangeModeEnum.Delete,
                        ki.EnteredText
                        );
                    e.Handled = true; //блокируем стандартную обработку
                }
                else if (AvalonTextEditor.SelectionLength > 0)
                {
                    ProcessLogic(
                        TextChangeModeEnum.SelectionReplace,
                        ki.EnteredText
                        );
                    e.Handled = true; //блокируем стандартную обработку
                }
            };

            #endregion
        }

        /// <summary>
        /// Attaches the prompt parser that <see cref="Parse"/> will use and registers each of its
        /// <see cref="MentionVisualLineGenerator"/> so their mention symbols (e.g. '@', '/') trigger
        /// completion popups in the editor. Must be called before the control is used.
        /// </summary>
        public void Setup(
            IParser parser
            )
        {
            if (parser is null)
            {
                throw new ArgumentNullException(nameof(parser));
            }

            _parser = parser;

            foreach (var generator in parser.Generators)
            {
                _generators.Add(generator);
                AvalonTextEditor.TextArea.TextView.ElementGenerators.Add(generator);
            }

        }

        /// <summary>
        /// Moves keyboard focus to the Embedillo text editor, used when the chat panel opens or is
        /// activated so the user can type immediately.
        /// </summary>
        public void MakeFocused()
        {
            AvalonTextEditor.Focus();
        }

        /// <summary>
        /// Runs the currently entered text through the configured <see cref="IParser"/>, resolving
        /// any mentions (files, commands, solution items) into a structured <see cref="Parsed"/>
        /// prompt ready to send to the model.
        /// </summary>
        public Parsed Parse()
        {
            if (_parser is null)
            {
                throw new InvalidOperationException("Setup this control before.");
            }
            var parsed = _parser.Parse(
                AvalonTextEditor.Text
                );
            return parsed;
        }

        /// <summary>
        /// Refreshes the bound <see cref="HintVisibility"/> and hint text bindings on the hint
        /// label, called after the editor's content or enabled state changes.
        /// </summary>
        public void UpdateHintStatus()
        {
            HintLabel.GetBindingExpression(System.Windows.Controls.TextBlock.VisibilityProperty).UpdateTarget();
            HintLabel.GetBindingExpression(System.Windows.Controls.TextBlock.TextProperty).UpdateTarget();
        }

        /// <summary>
        /// Opens the AvalonEdit completion popup for the given mention generator right after its
        /// anchor character (e.g. '@') was typed, populating it with the generator's suggestions
        /// and closing it once the caret moves past the typed token or no suggestion matches.
        /// </summary>
        private async Task ShowCompletionWindowAsync(
            MentionVisualLineGenerator generator
            )
        {
            var suggestions = await generator.GetSuggestionsAsync();
            if (suggestions is null || suggestions.Count == 0)
            {
                return;
            }

            var startPosition = AvalonTextEditor.CaretOffset - 1;
            var completionWindow = new CompletionWindow(AvalonTextEditor.TextArea);
            
            completionWindow.StartOffset = startPosition + 1;
            completionWindow.EndOffset = AvalonTextEditor.CaretOffset;
            completionWindow.Width = 50 + suggestions.Max(s => s.PublicData.Length) * 7;

            foreach (var suggestion in suggestions)
            {
                completionWindow.CompletionList.CompletionData.Add(
                    new CompletionData(suggestion)
                );
            }

            void KeyUpMethod(object sender, KeyEventArgs e)
            {
                try
                {
                    if (AvalonTextEditor.CaretOffset < startPosition)
                    {
                        //курсор ушел левее текста, для которого показаны подсказки
                        //убираем окно подсказок
                        AvalonTextEditor.KeyUp -= KeyUpMethod;
                        completionWindow.Close();
                        return;
                    }

                    var text = AvalonTextEditor.Text.Substring(
                        startPosition + 1,
                        AvalonTextEditor.CaretOffset - startPosition - 1
                        );

                    //MainWindow.HelpLabel1Static.Content = text;

                    var ltext = text.ToLower();

                    if (suggestions.All(j => !j.PublicData.ToLower().Contains(ltext)))
                    {
                        AvalonTextEditor.KeyUp -= KeyUpMethod;
                        completionWindow.Close();
                        return;
                    }
                }
                catch (Exception excp)
                {
                    excp.ActivityLogException();

                    AvalonTextEditor.KeyUp -= KeyUpMethod;
                    completionWindow.Close();
                }
            }

            AvalonTextEditor.KeyUp += KeyUpMethod;

            completionWindow.Show();
        }

        /// <summary>
        /// Applies a backspace/delete/ctrl-variant edit to the AvalonEdit document, either replacing
        /// the current selection with <paramref name="insertText"/> or deleting the character/word
        /// range computed by <see cref="CalculateDeleteCharCount"/> so mention tokens and CRLF pairs
        /// are removed as a single unit rather than character by character.
        /// </summary>
        private void ProcessLogic(
            TextChangeModeEnum changeMode,
            string insertText
            )
        {
            var document = AvalonTextEditor.Document;

            var selectionStart = AvalonTextEditor.SelectionStart;
            var selectionLength = AvalonTextEditor.SelectionLength;
            if (selectionLength > 0)
            {
                //текст был выделен

                //удаляем текст
                document.Replace(selectionStart, selectionLength, insertText);

                //убираем выделение
                AvalonTextEditor.SelectionLength = 0;

                //переводим каретку ЗА введенный символ
                AvalonTextEditor.CaretOffset = AvalonTextEditor.CaretOffset + insertText.Length;

                AvalonTextEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
            }
            else
            {
                //текст не был выделен
                var caretOffset = AvalonTextEditor.CaretOffset;

                //проверить, затрагивает ли правка какой-нибудь контрол
                var (deleteOffset, deleteCount) = CalculateDeleteCharCount(changeMode, document.Text, caretOffset);
                if (deleteCount > 0)
                {
                    //удаляем текст
                    document.Replace(deleteOffset, deleteCount, string.Empty);

                    AvalonTextEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
                }
            }
        }

        private static (int leftOffset, int length) CalculateDeleteCharCount(
            TextChangeModeEnum changeMode,
            string documentText,
            int caretOffset
            )
        {
            switch (changeMode)
            {
                case TextChangeModeEnum.Backspace:
                    {
                        if (documentText.Length == 0)
                        {
                            return (caretOffset, 0);
                        }
                        if (caretOffset == 0)
                        {
                            return (caretOffset, 0);
                        }

                        var charDeleteCount = 1;
                        var currentPosition = caretOffset - 1;

                        if (documentText[currentPosition] == '\n')
                        {
                            if (currentPosition > 0)
                            {
                                if (documentText[currentPosition - 1] == '\r')
                                {
                                    currentPosition--;
                                    charDeleteCount++;
                                }
                            }
                        }

                        return (currentPosition, charDeleteCount);
                    }
                case TextChangeModeEnum.CtrlBackspace:
                    {
                        if (documentText.Length == 0)
                        {
                            return (caretOffset, 0);
                        }
                        if (caretOffset == 0)
                        {
                            return (caretOffset, 0);
                        }

                        var charDeleteCount = 0;

                        caretOffset = caretOffset - 1;
                        while (caretOffset > 0)
                        {
                            if (!char.IsWhiteSpace(documentText[caretOffset]))
                            {
                                break;
                            }

                            charDeleteCount++;
                            caretOffset--;
                        }
                        while (caretOffset > 0)
                        {
                            if (char.IsWhiteSpace(documentText[caretOffset]))
                            {
                                break;
                            }

                            charDeleteCount++;
                            caretOffset--;
                        }

                        if (caretOffset > 0)
                        {
                            if (documentText[caretOffset] == '\n')
                            {
                                if (caretOffset > 1)
                                {
                                    if (documentText[caretOffset - 1] == '\r')
                                    {
                                        caretOffset--;
                                        charDeleteCount++;
                                    }
                                }
                            }
                        }
                        return (caretOffset, charDeleteCount);
                    }
                case TextChangeModeEnum.Delete:
                    {
                        if (documentText.Length == 0)
                        {
                            return (caretOffset, 0);
                        }
                        if (documentText.Length == caretOffset)
                        {
                            return (caretOffset, 0);
                        }

                        var charDeleteCount = 1;

                        if (documentText[caretOffset] == '\r')
                        {
                            if (documentText.Length > caretOffset + 1)
                            {
                                if (documentText[caretOffset + 1] == '\n')
                                {
                                    charDeleteCount++;
                                }
                            }
                        }

                        return (caretOffset, charDeleteCount);
                    }
                case TextChangeModeEnum.CtrlDelete:
                    {
                        if (documentText.Length == 0)
                        {
                            return (caretOffset, 0);
                        }
                        if (documentText.Length == caretOffset)
                        {
                            return (caretOffset, 0);
                        }

                        var charDeleteCount = 1;

                        var currentPosition = caretOffset + 1;
                        while (currentPosition < documentText.Length)
                        {
                            if (char.IsWhiteSpace(documentText[currentPosition]))
                            {
                                break;
                            }

                            charDeleteCount++;
                            currentPosition++;
                        }
                        while (currentPosition < documentText.Length)
                        {
                            if (!char.IsWhiteSpace(documentText[currentPosition]))
                            {
                                break;
                            }

                            charDeleteCount++;
                            currentPosition++;
                        }

                        if (documentText.Length > currentPosition)
                        {
                            if (documentText[currentPosition] == '\r')
                            {
                                if (documentText.Length > currentPosition + 1)
                                {
                                    if (documentText[currentPosition + 1] == '\n')
                                    {
                                        charDeleteCount++;
                                    }
                                }
                            }
                        }

                        return (caretOffset, charDeleteCount);
                    }
                case TextChangeModeEnum.SelectionReplace:
                default:
                    throw new InvalidOperationException("Unexpected branch!");
            }
        }

        private void EmbedilloInternalName_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateHintStatus();
        }

        private static void OnControlEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as EmbedilloControl;
            control.UpdateHintStatus();
        }
    }
}
