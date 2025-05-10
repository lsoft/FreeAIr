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
        private readonly ControlPositionManager _controlManager;
        private readonly List<MentionVisualLineGenerator> _generators = new();

        public static readonly DependencyProperty EnterCommandProperty =
            DependencyProperty.Register(
                nameof(EnterCommand),
                typeof(ICommand),
                typeof(EmbedilloControl));

        public ICommand EnterCommand
        {
            get => (ICommand)GetValue(EnterCommandProperty);
            set => SetValue(EnterCommandProperty, value);
        }

        public EmbedilloControl()
        {
            _controlManager = new ControlPositionManager();

            InitializeComponent();

            AvalonTextEditor.TextArea.TextEntered += (object sender, TextCompositionEventArgs e) =>
            {
                var generator = _generators.FirstOrDefault(g => e.Text == g.AnchorSymbol.ToString());
                if (generator is not null)
                {
                    ShowCompletionWindowAsync(generator)
                        .FileAndForget(nameof(ShowCompletionWindowAsync));
                }
            };

            AvalonTextEditor.Document.Changed += (sender, e) =>
            {
                foreach (var change in e.OffsetChangeMap)
                {
                    //смещение изменений влияет на позиции контроллов
                    int offsetDelta = change.RemovalLength - change.InsertionLength;

                    for (int i = 0; i < _controlManager.Positions.Count; i++)
                    {
                        var info = _controlManager.Positions[i];

                        //обновляем позиции после изменений в документе
                        if (info.Offset >= change.Offset + change.RemovalLength)
                        {
                            _controlManager.ReplaceControl(
                                info,
                                info.Offset - offsetDelta,
                                info.Length
                                );
                        }
                    }
                }
            };

            AvalonTextEditor.PreviewKeyDown += (object sender, KeyEventArgs e) =>
            {
                var ki = e.Key.IsTextChangedCombination(e.KeyboardDevice.Modifiers);
                if (!ki.IsTextChangedCombination || ki.EnteredText is null)
                {
                    return;
                }

                if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.Back)
                {
                    //ctrl + backspace не будем обрабатывать
                    //TODO: но неплохо бы сделать
                    e.Handled = true;
                    return;
                }
                if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.Delete)
                {
                    //ctrl + delete не будем обрабатывать
                    //TODO: но неплохо бы сделать
                    e.Handled = true;
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

                //если действие пользователя связано с копированием В буфер обмена
                //сразу обработаем эту операцию
                ki.PostProcessCopyToClipboard(
                    AvalonTextEditor.SelectedText
                    );

                if (e.Key == Key.Back)
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

        public void AddVisualLineGeneratorFactory(
            params IMentionVisualLineGeneratorFactory[] generatorFactories
            )
        {
            if (generatorFactories is null)
            {
                throw new ArgumentNullException(nameof(generatorFactories));
            }

            foreach (var generatorFactory in generatorFactories)
            {
                var generator = generatorFactory.Create(_controlManager);
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

                //удаляем контролы, которые попали в выделение
                for (var offset = selectionStart; offset < (selectionStart + selectionLength); offset++)
                {
                    var info = _controlManager.TryGetNearControl(offset);
                    if (info is not null)
                    {
                        _controlManager.RemoveControlWithOffset(info.Offset);
                    }
                }

                //удаляем текст
                document.Replace(selectionStart, selectionLength, insertText);

                //убираем выделение
                AvalonTextEditor.SelectionLength = 0;

                //переводим каретку ЗА введенный символ
                AvalonTextEditor.CaretOffset = AvalonTextEditor.CaretOffset + insertText.Length;
            }
            else
            {
                //текст не был выделен
                var caretOffset = AvalonTextEditor.CaretOffset;

                //проверить, затрагивает ли правка какой-нибудь контрол
                var info = _controlManager.TryGetNearControl(caretOffset);
                if (info is not null)
                {
                    //да, затрагивает
                    //удаляем символ из контрола
                    switch (changeMode)
                    {
                        case TextChangeModeEnum.Backspace:
                            if (document.TextLength >= caretOffset)
                            {
                                var charDeleteCount = CalculateDeleteCharCount(changeMode, document.Text, caretOffset);
                                if (charDeleteCount > 0)
                                {
                                    document.Replace(caretOffset - charDeleteCount, charDeleteCount, string.Empty);
                                    _controlManager.RemoveControlWithOffset(info.Offset);
                                    AvalonTextEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
                                }
                            }
                            break;
                        case TextChangeModeEnum.Delete:
                            if (document.TextLength > caretOffset)
                            {
                                var charDeleteCount = CalculateDeleteCharCount(changeMode, document.Text, caretOffset);
                                if (charDeleteCount > 0)
                                {
                                    document.Replace(caretOffset, charDeleteCount, string.Empty);
                                    _controlManager.RemoveControlWithOffset(info.Offset);
                                    AvalonTextEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
                                }
                            }
                            break;
                    }
                }
                else
                {
                    //правка не затрагивает контрол
                    //удаляем символ из текста
                    switch (changeMode)
                    {
                        case TextChangeModeEnum.Backspace:
                            if (caretOffset > 0 && document.TextLength >= caretOffset)
                            {
                                var charDeleteCount = CalculateDeleteCharCount(changeMode, document.Text, caretOffset);
                                if (charDeleteCount > 0)
                                {
                                    document.Replace(caretOffset - charDeleteCount, charDeleteCount, string.Empty);
                                    AvalonTextEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
                                }
                            }
                            break;
                        case TextChangeModeEnum.Delete:
                            if (document.TextLength > caretOffset)
                            {
                                var charDeleteCount = CalculateDeleteCharCount(changeMode, document.Text, caretOffset);
                                if (charDeleteCount > 0)
                                {
                                    document.Replace(caretOffset, charDeleteCount, string.Empty);
                                    AvalonTextEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
                                }
                            }
                            break;
                    }
                }
            }
        }

        private static int CalculateDeleteCharCount(
            TextChangeModeEnum changeMode,
            string documentText,
            int caretOffset
            )
        {
            switch (changeMode)
            {
                case TextChangeModeEnum.Backspace:
                    {
                        if (caretOffset == 0)
                        {
                            return 0;
                        }

                        var charDeleteCount = 1;

                        var deletedChar = documentText.Substring(caretOffset - 1, 1);
                        if (deletedChar == "\n")
                        {
                            var previousChar = documentText.Substring(caretOffset - 2, 1);
                            if (previousChar == "\r")
                            {
                                charDeleteCount = 2;
                            }
                        }

                        return charDeleteCount;
                    }
                case TextChangeModeEnum.Delete:
                    {
                        if (documentText.Length == caretOffset - 1)
                        {
                            return 0;
                        }

                        var charDeleteCount = 1;
                        var deletedChar = documentText.Substring(caretOffset, 1);
                        if (deletedChar == "\r")
                        {
                            if (documentText.Length > caretOffset + 1)
                            {
                                var nextChar = documentText.Substring(caretOffset + 1, 1);
                                if (nextChar == "\n")
                                {
                                    charDeleteCount = 2;
                                }
                            }
                        }
                        return charDeleteCount;
                    }
                case TextChangeModeEnum.SelectionReplace:
                default:
                    throw new InvalidOperationException("Unexpected branch!");
            }
        }

        public ParsedAnswer ParseAnswer()
        {
            var parser = new AnswerParser(
                _generators
                );

            var parsedAnswer = parser.Parse(
                AvalonTextEditor.Text
                );
            return parsedAnswer;
        }
    }
}
