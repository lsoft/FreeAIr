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
    public partial class EmbedilloControl : UserControl
    {
        private readonly List<MentionVisualLineGenerator> _generators = new();
        private IParser? _parser;

        public static readonly DependencyProperty EnterCommandProperty =
            DependencyProperty.Register(
                nameof(EnterCommand),
                typeof(ICommand),
                typeof(EmbedilloControl));

        public static readonly DependencyProperty ControlEnabledProperty =
            DependencyProperty.Register(
                nameof(ControlEnabled),
                typeof(bool),
                typeof(EmbedilloControl),
                new PropertyMetadata(default(bool), OnControlEnabledChanged)
                );

        public static readonly DependencyProperty MaxHeightProperty =
            DependencyProperty.Register(
                nameof(MaxHeight),
                typeof(int),
                typeof(EmbedilloControl),
                new PropertyMetadata(default(int), OnControlEnabledChanged)
                );

        public int MaxHeight
        {
            get => (int)GetValue(MaxHeightProperty);
            set => SetValue(MaxHeightProperty, value);
        }

        public bool ControlEnabled
        {
            get => (bool)GetValue(ControlEnabledProperty);
            set => SetValue(ControlEnabledProperty, value);
        }

        private static void OnControlEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as EmbedilloControl;
            control.UpdateHintStatus();
        }

        public string HintText
        {
            get;
            set;
        }

        public ICommand EnterCommand
        {
            get => (ICommand)GetValue(EnterCommandProperty);
            set => SetValue(EnterCommandProperty, value);
        }

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

        public EmbedilloControl()
        {
            InitializeComponent();

            ControlEnabled = true;

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
        }

        private void UpdateHintStatus()
        {
            HintLabel.GetBindingExpression(System.Windows.Controls.Label.VisibilityProperty).UpdateTarget();
            HintLabel.GetBindingExpression(System.Windows.Controls.Label.ContentProperty).UpdateTarget();
        }

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

        public void MakeFocused()
        {
            AvalonTextEditor.Focus();
        }

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
                    //todo log
                    AvalonTextEditor.KeyUp -= KeyUpMethod;
                    completionWindow.Close();
                }
            }

            AvalonTextEditor.KeyUp += KeyUpMethod;

            completionWindow.Show();
        }

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

        private void EmbedilloInternalName_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateHintStatus();
        }
    }
}
