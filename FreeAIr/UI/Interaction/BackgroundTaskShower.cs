using FreeAIr.Interaction;
using FreeAIr.UI.Windows;
using System.ComponentModel.Composition;
using System.Windows;

namespace FreeAIr.UI.Interaction
{
    /// <summary>
    /// Shows a background task in the modal <see cref="WaitForTaskWindow"/>, which is the only
    /// implementation of <see cref="IBackgroundTaskShower"/> the extension ships.
    /// </summary>
    [Export(typeof(IBackgroundTaskShower))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class BackgroundTaskShower : IBackgroundTaskShower
    {
        /// <inheritdoc/>
        public async Task ShowAsync(BackgroundTask backgroundTask)
        {
            if (backgroundTask is null)
            {
                throw new ArgumentNullException(nameof(backgroundTask));
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var window = new WaitForTaskWindow(
                backgroundTask
                );
            await window.ShowDialogAsync();
        }
    }
}
