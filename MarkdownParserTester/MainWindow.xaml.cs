using MarkdownParser.Antlr.Answer;
using MarkdownParser.Antlr.Answer.Parts;
using System.Windows;
using System.Windows.Documents;
using WpfHelpers;

namespace MarkdownParserTester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public FlowDocument Document
        {
            get;
        } = new();

        public AdditionalCommandContainer AdditionalCommandContainer
        {
            get;
            private set;
        }

        public MainWindow()
        {
            CreateDocument();

            InitializeComponent();
        }

        private void CreateDocument()
        {
            AdditionalCommandContainer = new();

            AdditionalCommandContainer.AddAdditionalCommand(
                new AdditionalCommand(
                    ConstantFontSizeProvider.Instance,
                    PartTypeEnum.Xml,
                    "⤢",
                    "Expand-Collapse",
                    new RelayCommand(
                        a =>
                        {
                            var xmlNodePart = a as XmlNodePart;
                            if (xmlNodePart is null)
                            {
                                return;
                            }

                            xmlNodePart.ExpandOrCollapse();
                        }),
                    null
                    )
                );
            AdditionalCommandContainer.AddAdditionalCommand(
                new AdditionalCommand(
                    ConstantFontSizeProvider.Instance,
                    PartTypeEnum.Xml,
                    "📋",
                    "Click to copy to clipboard",
                    new RelayCommand(
                        a =>
                        {
                            var xmlNodePart = a as XmlNodePart;
                            if (xmlNodePart is null)
                            {
                                return;
                            }

                            if (!string.IsNullOrEmpty(xmlNodePart.Body))
                            {
                                Clipboard.SetText(xmlNodePart.Body);
                            }
                        }),
                    null
                    )
                );
            AdditionalCommandContainer.AddAdditionalCommand(
                new AdditionalCommand(
                    ConstantFontSizeProvider.Instance,
                    PartTypeEnum.CodeLine | PartTypeEnum.CodeBlock | PartTypeEnum.Url,
                    "📋",
                    "Click to copy to clipboard",
                    new RelayCommand(
                        a =>
                        {
                            var code = a as string;
                            if (!string.IsNullOrEmpty(code))
                            {
                                Clipboard.SetText(code);
                            }
                        }),
                    null
                    )
                );

            var answerParser = new DirectMarkdownParser(
                ConstantFontSizeProvider.Instance
                );

            //var body = System.IO.File.ReadAllText(@"C:\temp\1.txt");
            //var parsedAnswer = answerParser.Parse(
            //    body
            //    );

            var parsedAnswer = answerParser.Parse(
"""
<mynode>One line xml node!</mynode>

and the multiline:

<think>
Multi
    line
        "thinking"
Wait, this is
    a fake LLM
                chat!
</think>

# Heading 1
## Heading 2
### Heading 3

<https://ya.ru>

#### Heading 4
##### Heading 5
###### Heading 6

Just a **regular text** with `codeblock with a space` and [link to my profile](https://github.com/lsoft).

![image](https://avatars.githubusercontent.com/u/5988558 "The author himself") .

```
        Unknown
    multiline
codeblock
```

and

```csharp
CSharp
    multiline
        codeblock
```

-------

And here is:

> quotation line 1
> quotation line 2
> quotation line 3

heh!


<think>
s => s
</think>

The table:

|   | h0 | h1 | h2 | h3 | h4 | h5 | h6 | h7 | h8 | h9 |
|---|---|---|---|---|---|---|---|---|---|---|
| 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1 | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 |
| 2 | 0 | 2 | 4 | 6 | 8 | 10 | 12 | 14 | 16 | 18 |





That's all, folks!



""".Replace("\r\n", "\n")
                );


            parsedAnswer.UpdateFlowDocument(
                Document,
                AdditionalCommandContainer,
                false
                );
        }
    }
}