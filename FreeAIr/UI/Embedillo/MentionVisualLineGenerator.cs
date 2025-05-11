using ICSharpCode.AvalonEdit.Rendering;
using System.Text.RegularExpressions;
using System.Windows;
using System.Linq;
using FreeAIr.Helper;
using System.Collections.Generic;
using FreeAIr.UI.Embedillo.Answer.Parser;
using Microsoft.VisualStudio.Imaging.Interop;

namespace FreeAIr.UI.Embedillo
{
    public interface IMentionVisualLineGeneratorFactory
    {
        MentionVisualLineGenerator Create(
            ControlPositionManager positionManager
            );
    }

    public abstract class MentionVisualLineGenerator : VisualLineElementGenerator
    {
        private readonly Regex _targetRegex;

        private static readonly Regex _threeSlashRegex = new Regex(
            @"\`\`\`[\s\S]*?\`\`\`",
            RegexOptions.Compiled
            );

        private static readonly Regex _oneSlashRegex = new Regex(
            @"\`[\s\S]*?\`",
            RegexOptions.Compiled
            );

        private readonly char _anchorSymbol;
        private readonly ControlPositionManager _controlPositionManager;

        public char AnchorSymbol => _anchorSymbol;

        public MentionVisualLineGenerator(
            char anchorSymbol,
            ControlPositionManager controlPositionManager
            )
        {
            _anchorSymbol = anchorSymbol;
            _controlPositionManager = controlPositionManager;

            _targetRegex = new Regex(
                @"(?<!\S)" + anchorSymbol + @"([\p{L}\p{M}0-9_:\\.@\-~\[\]]+)"
                );
        }

        public override int GetFirstInterestedOffset(int startOffset)
        {
            var document = CurrentContext.TextView.Document;
            var text = document.Text;

            text = PrepareTextForSearchingAnchor(text);

            var targetMatch = _targetRegex.Match(text, startOffset);
            return targetMatch.Success ? targetMatch.Index : -1;
        }

        public static string PrepareTextForSearchingAnchor(string text)
        {
            //не обрабатывать anchor внутри
            //```
            //тут
            //```
            foreach (Match match in _threeSlashRegex.Matches(text))
            {
                foreach (var capture in match.Captures.OrderBy(c => c.Index))
                {
                    text = text.Substring(0, capture.Index)
                        + new string('*', capture.Length)
                        + text.Substring(capture.Index + capture.Length)
                        ;
                }
            }

            //не обрабатывать anchor внутри `тут`
            foreach (Match match in _oneSlashRegex.Matches(text))
            {
                foreach (var capture in match.Captures.OrderBy(c => c.Index))
                {
                    text = text.Substring(0, capture.Index)
                        + new string('*', capture.Length)
                        + text.Substring(capture.Index + capture.Length)
                        ;
                }
            }

            return text;
        }

        public override VisualLineElement? ConstructElement(int offset)
        {
            var document = CurrentContext.TextView.Document;
            var text = document.Text;

            var remainingText = text.Substring(offset);
            var match = _targetRegex.Match(remainingText);

            if (!match.Success || match.Index != 0)
            {
                return null;
            }

            var mentionText = _anchorSymbol + match.Groups[1].Value;

            var control = CreateControl(mentionText);

            var element = new InlineObjectElement(
                mentionText.Length,
                control
                );

            _controlPositionManager.AddControl(offset, mentionText.Length);

            return element;
        }

        protected abstract UIElement CreateControl(string mentionText);

        public abstract System.Threading.Tasks.Task<List<ISuggestion>> GetSuggestionsAsync();
        
        public abstract IAnswerPart CreatePart(string partPayload);
    }

    public interface ISuggestion
    {
        ImageMoniker Image
        {
            get;
        }

        string FullData
        {
            get;
        }

        string PublicData
        {
            get;
        }
    }
}