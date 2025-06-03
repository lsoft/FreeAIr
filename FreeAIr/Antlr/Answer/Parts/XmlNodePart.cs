using System.Collections.Generic;
using System.Windows.Documents;
using System.Windows.Media;

namespace FreeAIr.Antlr.Answer.Parts
{
    public sealed class XmlNodePart : IPart
    {
        public PartTypeEnum Type => PartTypeEnum.Xml;

        public string Text
        {
            get;
        }
        public string NodeName
        {
            get;
        }

        public string Body
        {
            get;
        }

        public XmlNodePart(
            string text,
            string nodeName,
            string body
            )
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (nodeName is null)
            {
                throw new ArgumentNullException(nameof(nodeName));
            }

            if (body is null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            Text = text;
            NodeName = nodeName;
            Body = body;
        }

        public object GetContextForAdditionalCommand()
        {
            return Body;
        }

        public IEnumerable<Inline> GetInlines(bool isInProgress)
        {
            yield return new Run
            {
                FontSize = 12,
                Foreground = Brushes.Red,
                Text = Text
            };
        }
    }
}
