using FreeAIr.UI.Windows;
using System.Collections.Generic;
using FreeAIr.Chat.Context;

namespace FreeAIr.UI.ViewModels
{
    /// <summary>
    /// Runs the "find related context items" search behind a chat context item on a background
    /// thread, showing a wait dialog while it looks for code the item depends on.
    /// </summary>
    public sealed class AddRelatedItemsToContextBackgroundTask : BackgroundTask
    {
        private readonly ChatContextItemViewModel _viewModel;

        /// <summary>
        /// The message shown in the wait dialog while the related-items search runs.
        /// </summary>
        public override string TaskDescription => FreeAIr.Resources.Resources.Please_wait_for_the_code_dependencies;

        /// <summary>
        /// The context items found to be related to the source item, populated once the
        /// background search completes; null while running or if it threw.
        /// </summary>
        public IReadOnlyList<IChatContextItem>? Result
        {
            get;
            private set;
        }

        /// <summary>
        /// Starts the background search for context items related to the item wrapped by
        /// <paramref name="viewModel"/>.
        /// </summary>
        public AddRelatedItemsToContextBackgroundTask(
            ChatContextItemViewModel viewModel
            )
        {
            if (viewModel is null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            _viewModel = viewModel;

            StartAsyncTask();
        }

        /// <summary>
        /// Searches for context items related to <see cref="_viewModel"/>'s item and stores them
        /// in <see cref="Result"/>.
        /// </summary>
        protected override async Task RunWorkingTaskAsync(
            )
        {
            //in case of exception set it null first
            Result = null;

            Result = await _viewModel.ContextItem.SearchRelatedContextItemsAsync();
        }
    }

}
