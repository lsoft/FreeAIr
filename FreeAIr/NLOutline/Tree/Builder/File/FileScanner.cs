using FreeAIr.Agents;
using FreeAIr.BLogic;
using FreeAIr.BLogic.Context.Item;
using FreeAIr.Embedding;
using FreeAIr.Git.Parser;
using FreeAIr.Shared.Helper;
using Microsoft.VisualStudio.ComponentModelHost;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.NLOutline.Tree.Builder.File
{
    public interface IFileScanner
    {
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
            List<SolutionItem> items,
            OutlineTree root
            );
    }

    /// <summary>
    /// Default file outline tree node scanner. Used for unknown language (or file type).
    /// LLM produces outlines actually.
    /// </summary>
    public sealed class FileScanner : IFileScanner
    {
        public static readonly FileScanner DefaultInstance = new();


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
            List<SolutionItem> items,
            OutlineTree root
            )
        {
            await BuildInternalAsync(
                agent,
                items,
                root
                );
        }

        private async Task BuildInternalAsync(
            Agent agent,
            List<SolutionItem> items,
            OutlineTree root
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

                var prompt = UserPrompt.CreateFileOutlinesPrompt(
                    item.FullPath
                    );
                chat.AddPrompt(prompt);

                var fileOutline = await chat.WaitForPromptCleanAnswerAsync(
                    Environment.NewLine
                    );
                if (string.IsNullOrEmpty(fileOutline))
                {
                    continue;
                }

                root.AddChild(
                    OutlineKindEnum.File,
                    item.FullPath,
                    fileOutline
                    );

                chat.ChatContext.RemoveItem(contextItem);
                chat.ArchiveAllPrompts();
            }
        }
    }

    ///// <summary>
    ///// C# outline tree node scanner. Used Roslyn to extract all outlines from source code.
    ///// </summary>
    //public sealed class CSharpFileScanner : IFileScanner
    //{
    //    public static readonly CSharpFileScanner Instance = new();

    //    public CSharpFileBuilder()
    //    {
    //        FileExtensions = [".cs"];
    //    }

    //    //protected override async Task<OutlineTree?> BuildInternalAsync()
    //    //{
    //    //    await base.BuildInternalAsync();


    //    //}
    //}


    public static class FileScannerFactory
    {
        public static readonly IReadOnlyList<IFileScanner> Scanners =
        [
            //CSharpFileBuilder.Instance
        ];

        public static async Task CreateFileTreeAsync(
            Agent agent,
            OutlineTree root,
            List<SolutionItem> items
            )
        {
            if (agent is null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            if (root is null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var dict = new Dictionary<IFileScanner, List<SolutionItem>>();
            dict.Add(FileScanner.DefaultInstance, []);
            Scanners.ForEach(f => dict.Add(f, []));

            foreach (var item in items)
            {
                var extension = new FileInfo(item.FullPath).Extension;
                foreach (var pair in dict)
                {
                    if (pair.Key.FileExtensions.Contains(extension))
                    {
                        pair.Value.Add(item);
                        continue;
                    }

                    dict[FileScanner.DefaultInstance].Add(item);
                }
            }

            foreach (var pair in dict.OrderBy(p => p.Key.GetType().Name))
            {
                await pair.Key.BuildAsync(
                    agent,
                    pair.Value,
                    root
                    );
            }
        }
    }
}
