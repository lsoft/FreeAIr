﻿using FreeAIr.BLogic.Context.Composer;
using FreeAIr.Helper;
using FreeAIr.UI.Embedillo.Answer.Parser;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Threading.Tasks;

namespace FreeAIr.BLogic.Context.Item
{
    public sealed class SolutionItemChatContextItem : IChatContextItem
    {
        public SelectedIdentifier SelectedIdentifier
        {
            get;
        }

        public string ContextUIDescription => SelectedIdentifier.FilePath + SelectedIdentifier.Selection?.ToString();

        public bool IsAutoFound
        {
            get;
        }

        public SolutionItemChatContextItem(
            SelectedIdentifier selectedIdentifier,
            bool isAutoFound
            )
        {
            if (selectedIdentifier is null)
            {
                throw new ArgumentNullException(nameof(selectedIdentifier));
            }

            SelectedIdentifier = selectedIdentifier;
            IsAutoFound = isAutoFound;
        }

        public bool IsSame(IChatContextItem other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is not SolutionItemChatContextItem otherDisk)
            {
                return false;
            }

            if (!SelectedIdentifier.Equals(
                    otherDisk.SelectedIdentifier
                    )
                )
            {
                return false;
            }

            return true;
        }

        public async Task OpenInNewWindowAsync()
        {
            await SelectedIdentifier.OpenInNewWindowAsync();
        }


        public async Task<string> AsContextPromptTextAsync()
        {
            if (!File.Exists(SelectedIdentifier.FilePath))
            {
                return $"`File {SelectedIdentifier.FilePath} does not found`";
            }

            var fi = new FileInfo(SelectedIdentifier.FilePath);

            if (SelectedIdentifier.Selection is not null)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var documentView = await VS.Documents.GetDocumentViewAsync(SelectedIdentifier.FilePath);
                var body = documentView.TextView.TextSnapshot.GetText(
                    SelectedIdentifier.Selection.GetVisualStudioSpan()
                    );

                return
                    Environment.NewLine
                    + "```"
                    + LanguageHelper.GetMarkdownLanguageCodeBlockNameBasedOnFileExtension(fi.Extension)
                    + Environment.NewLine
                    + body
                    + Environment.NewLine
                    + "```"
                    + Environment.NewLine
                    ;
            }
            else
            {
                return
                    Environment.NewLine
                    + $"Text of the file `{SelectedIdentifier.FilePath}`:"
                    + Environment.NewLine
                    + Environment.NewLine
                    + "```"
                    + LanguageHelper.GetMarkdownLanguageCodeBlockNameBasedOnFileExtension(fi.Extension)
                    + Environment.NewLine
                    + System.IO.File.ReadAllText(SelectedIdentifier.FilePath)
                    + Environment.NewLine
                    + "```"
                    + Environment.NewLine
                    ;
            }
        }

        public void ReplaceWithText(string body)
        {
            if (body is null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            var lineEnding = LineEndingHelper.Actual.OpenDocumentAndGetLineEnding(
                SelectedIdentifier.FilePath
                );


            File.WriteAllText(
                SelectedIdentifier.FilePath,
                body.WithLineEnding(lineEnding)
                );
        }

        public async Task<IReadOnlyList<IChatContextItem>> SearchRelatedContextItemsAsync()
        {
            var contextItems = (await ContextComposer.ComposeFromFilePathAsync(
                SelectedIdentifier.FilePath
                )).ConvertToChatContextItem();
            return contextItems;
        }
    }

}
