using FreeAIr.Agents;
using FreeAIr.BLogic;
using FreeAIr.BLogic.Context.Item;
using FreeAIr.Embedding;
using FreeAIr.Git.Parser;
using FreeAIr.Helper;
using FreeAIr.Shared.Helper;
using Microsoft.VisualStudio.ComponentModelHost;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.NLOutline.Tree.Builder.File
{
    public interface IFileScanner
    {
        int Order
        {
            get;
        }

        string Name
        {
            get;
        }

        string Description
        {
            get;
        }

        IReadOnlyList<string> FileExtensions
        {
            get;
        }

        Task BuildAsync(
            Agent agent,
            string rootPath,
            List<SolutionItem> items,
            OutlineNode root
            );
    }

    /// <summary>
    /// Default file outline tree node scanner. Used for unknown language (or file type).
    /// LLM produces outlines actually.
    /// </summary>
    [Export(typeof(IFileScanner))]
    public sealed class FileScanner : IFileScanner
    {
        public int Order => int.MaxValue;
        
        public string Name => "Default file scanner";

        public string Description => "Default scanner for NLO. It asks LLM to produce NLO tree for the file.";

        public IReadOnlyList<string> FileExtensions
        {
            get;
        }

        public FileScanner()
        {
            FileExtensions = new List<string>();
        }

        public async Task BuildAsync(
            Agent agent,
            string rootPath,
            List<SolutionItem> items,
            OutlineNode root
            )
        {
            await BuildInternalAsync(
                agent,
                rootPath,
                items,
                root
                );
        }

        private async Task BuildInternalAsync(
            Agent agent,
            string rootPath,
            List<SolutionItem> items,
            OutlineNode root
            )
        {
            if (agent is null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (root is null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));

            var chatContainer = componentModel.GetService<ChatContainer>();

            var chat = await chatContainer.StartChatAsync(
                new ChatDescription(
                    ChatKindEnum.Discussion,
                    null
                    ),
                null,
                ChatOptions.NoToolAutoProcessedTextResponse(agent)
                );

            if (chat is null)
            {
                return;
            }

            foreach (var item in items)
            {
                var contextItem = new SolutionItemChatContextItem(
                    new UI.Embedillo.Answer.Parser.SelectedIdentifier(
                        item.FullPath,
                        null
                        ),
                    false,
                    AddLineNumbersMode.NotRequired
                    );
                chat.ChatContext.AddItem(contextItem);

                var fileOutline = await ProcessOutlinePromptAsync(chat, item);
                if (!string.IsNullOrEmpty(fileOutline))
                {
                    root.AddChild(
                        OutlineKindEnum.File,
                        item.FullPath.MakeRelativeAgainst(rootPath),
                        fileOutline
                        );
                }

                chat.ChatContext.RemoveItem(contextItem);
                chat.ArchiveAllPrompts();
            }
        }

        private static async Task<string> ProcessOutlinePromptAsync(Chat chat, SolutionItem item)
        {
            var prompt = UserPrompt.CreateFileOutlinesPrompt(
                item.FullPath
                );
            chat.AddPrompt(prompt);

            var fileOutline = await chat.WaitForPromptCleanAnswerAsync(
                Environment.NewLine
                );

            return fileOutline;
        }
    }

    ///// <summary>
    ///// C# outline tree node scanner. Used Roslyn to extract all outlines from source code.
    ///// </summary>
    //public sealed class CSharpFileScanner : IFileScanner
    //{
    //    public CSharpFileBuilder()
    //    {
    //        FileExtensions = [".cs"];
    //    }

    //    //protected override async Task<OutlineTree?> BuildInternalAsync()
    //    //{
    //    //    await base.BuildInternalAsync();


    //    //}
    //}


    [Export(typeof(FileOutlineTreeProcessor))]
    public sealed class FileOutlineTreeProcessor
    {
        private readonly IFileScanner[] _scanners;

        [ImportingConstructor]
        public FileOutlineTreeProcessor(
            [ImportMany] IFileScanner[] scanners
            )
        {
            if (scanners is null)
            {
                throw new ArgumentNullException(nameof(scanners));
            }

            _scanners = scanners;
        }


        public async Task CreateFileTreesAsync(
            TreeBuilderParameters parameters,
            string rootPath,
            OutlineNode newOutlineRoot,
            List<SolutionItem> fileItems
            )
        {
            if (parameters is null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (newOutlineRoot is null)
            {
                throw new ArgumentNullException(nameof(newOutlineRoot));
            }

            if (fileItems is null)
            {
                throw new ArgumentNullException(nameof(fileItems));
            }

            var splittedItems = SplitItemsByScanners(fileItems);

            foreach (var pair in splittedItems)
            {
                var solutionItems = pair.SolutionItems;

                //use the old file node if the current file node is not checked
                if (parameters.OldOutlineRoot is not null)
                {
                    for (var i = solutionItems.Count - 1; i >= 0; i--)
                    {
                        var solutionItem = solutionItems[i];
                        if (parameters.TryGetFileOutlineNode(
                            solutionItem.FullPath.MakeRelativeAgainst(rootPath),
                            out var oldOutlineNode)
                            )
                        {
                            newOutlineRoot.AddChild(
                                oldOutlineNode
                                );
                            solutionItems.RemoveAt(i);
                        }
                    }
                }

                if (solutionItems.Count > 0)
                {
                    await pair.FileScanner.BuildAsync(
                        parameters.Agent,
                        rootPath,
                        solutionItems,
                        newOutlineRoot
                        );
                }
            }
        }

        private List<(IFileScanner FileScanner, List<SolutionItem> SolutionItems)> SplitItemsByScanners(
            List<SolutionItem> fileItems
            )
        {
            var list = _scanners
                .OrderBy(f => f.Order)
                .Select(f => (FileScanner: f, SolutionItems: new List<SolutionItem>()))
                .ToList()
                ;

            foreach (var item in fileItems)
            {
                var extension = new FileInfo(item.FullPath).Extension;
                foreach (var pair in list)
                {
                    if (pair.FileScanner.FileExtensions.Count == 0
                        || pair.FileScanner.FileExtensions.Contains(extension)
                        )
                    {
                        pair.SolutionItems.Add(item);
                        break;
                    }
                }
            }

            return list;
        }
    }
}
