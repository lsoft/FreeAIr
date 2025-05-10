using FreeAIr.BLogic;
using FreeAIr.Helper;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FreeAIr.UI.Embedillo.Answer.Parser
{
    public sealed class ParsedAnswer
    {
        private readonly List<IAnswerPart> _parts = new();

        public IReadOnlyList<IAnswerPart> Parts => _parts;

        public void AppendPart(IAnswerPart part)
        {
            if (part is null)
            {
                throw new ArgumentNullException(nameof(part));
            }

            _parts.Add(part);
        }

        public string ComposeStringRepresentation()
        {
            if (_parts.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            foreach (var part in _parts)
            {
                var stringPart = part.AsPromptString();
                sb.Append(stringPart);
            }

            return sb.ToString();
        }
    }

    public sealed class AnswerPartReceiver
    {
        private readonly List<MentionVisualLineGenerator> _generators;
        
        private readonly ParsedAnswer _parsedAnswer = new();
        private readonly StringBuilder _sb = new();
        private readonly SlashCounter _slashCounter = new();

        public AnswerPartReceiver(
            List<MentionVisualLineGenerator> generators
            )
        {
            if (generators is null)
            {
                throw new ArgumentNullException(nameof(generators));
            }

            _generators = generators;
        }

        public ParsedAnswer GetParsedAnswer()
        {
            return _parsedAnswer;
        }

        public void AcceptPart(char symbol)
        {
            if (symbol != '\0' && _sb.Length > 0)
            {
                //процессим пустые "слова"

                var previousWhitespace = char.IsWhiteSpace(_sb[_sb.Length - 1]);
                var currentWhiteSpace = char.IsWhiteSpace(symbol);
                if (previousWhitespace && currentWhiteSpace)
                {
                    //еще один пустой символ в коллекцию пустых символов
                    _sb.Append(symbol);
                    return;
                }
                if (previousWhitespace && !currentWhiteSpace)
                {
                    //пустые символы кончились, начались слова
                    var word = _sb.ToString();

                    var part = new StringAnswerPart(word);
                    _parsedAnswer.AppendPart(part);
                    
                    _sb.Clear();
                    _sb.Append(symbol);
                    
                    return;
                }
            }

            AnalyzeCollectedWord(symbol);

            if (symbol != '\0')
            {
                _sb.Append(symbol);
                _slashCounter.AcceptChar(symbol);
            }
        }

        private void AnalyzeCollectedWord(
            char symbol
            )
        {
            if (!_slashCounter.IsInSlash)
            {
                //we are outside of slash blocks:
                //`asd`
                //or
                //```
                //asd
                //```

                if (char.IsWhiteSpace(symbol) || symbol == '\0')
                {
                    //previous word has ended

                    var word = _sb.ToString();

                    var generator = _generators.FirstOrDefault(
                        g => word.StartsWith(g.AnchorSymbol.ToString())
                        );
                    if (generator is null)
                    {
                        var part = new StringAnswerPart(word);
                        _parsedAnswer.AppendPart(part);
                        _sb.Clear();
                    }
                    else
                    {
                        var partPayload = word.Substring(1);
                        var part = generator.CreatePart(partPayload);
                        _parsedAnswer.AppendPart(part);
                        _sb.Clear();
                    }
                }
            }
            else
            {
                var word = _sb.ToString();

                if (symbol == '\0')
                {
                    var part = new StringAnswerPart(word);
                    _parsedAnswer.AppendPart(part);
                    _sb.Clear();
                }
            }
        }

        private sealed class SlashCounter
        {
            private int _counter = 0;
            private int _increment = 1;

            public bool IsInSlash => _counter > 0;

            public void AcceptChar(char symbol)
            {
                if (symbol == '`')
                {
                    _counter += _increment;

                    if (_counter < 0)
                    {
                        //этого не должно быть, но вдруг будет такое:
                        //```
                        //bla
                        //````
                        //blabla
                        _counter = 0;
                    }
                    if (_counter > 3)
                    {
                        //этого не должно быть, но вдруг будет такое:
                        //````
                        //bla
                        //```
                        //blabla
                        _counter = 3;
                    }
                }
                else
                {
                    if (_counter > 0)
                    {
                        _increment = -1;
                    }
                    else
                    {
                        _increment = 1;
                    }
                }
            }
        }

    }


    public interface IAnswerPart
    {
        /// <summary>
        /// Как должен выглядеть этот парт непосредственно
        /// в теле промпта.
        /// например, файловый парт в теле промпта должен выглядеть просто
        /// как путь до файла (контекст даст LLM полный текст файла).
        /// </summary>
        string AsPromptString();
    }

    public sealed class StringAnswerPart : IAnswerPart
    {
        public string Text
        {
            get;
        }

        public StringAnswerPart(string text)
        {
            Text = text;
        }

        public string AsPromptString()
        {
            return Text;
        }

    }

    public sealed class SourceFileAnswerPart : IAnswerPart
    {
        public string FilePath
        {
            get;
        }

        public SourceFileAnswerPart(
            string filePath
            )
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            FilePath = filePath;
        }

        public string AsPromptString()
        {
            return FilePath;
        }

        public bool IsFileExists()
        {
            return File.Exists(FilePath);
        }
    }

    public sealed class CommandAnswerPart : IAnswerPart
    {
        public ChatKindEnum Kind
        {
            get;
        }

        public CommandAnswerPart(
            ChatKindEnum kind
            )
        {
            Kind = kind;
        }

        public string AsPromptString()
        {
            return Kind.AsPromptString();
        }

    }
}
