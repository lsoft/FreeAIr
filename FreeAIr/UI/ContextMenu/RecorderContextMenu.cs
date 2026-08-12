using FreeAIr.Helper;
using FreeAIr.Options2;
using FreeAIr.Options2.Support;
using FreeAIr.Record;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FreeAIr.UI.ContextMenu
{
    /// <summary>
    /// Builds the combined Visual Studio context menu for the voice recording feature: choosing
    /// which recorder to use, which support action should post-process the transcript, and toggling
    /// whether recording is enabled.
    /// </summary>
    public static class RecorderContextMenu
    {
        /// <summary>
        /// Shows the recorder/post-process picker menu, listing every discovered
        /// <see cref="IRecorderFactory"/> and every support action scoped to
        /// <see cref="SupportScopeEnum.RecordPostProcess"/>, plus the enable/disable and help entries.
        /// </summary>
        public static async Task<object?> OpenRecorderAndPostProcessMenuAsync(
            string? chosenRecorderName,
            string? chosenPostProcessActionName
            )
        {

            var builder = VisualStudioContextMenuCommandBridge.BuildMenuItems();

            builder.AddTitle("Choose recorder:");
            var recorderFactories = await ObtainRecorderFactoriesAsync();
            builder.AddItems(
                recorderFactories
                    .ConvertAll(a => (a.Name, a.Name == chosenRecorderName, a as object))
                );

            var filteredActions = await FreeAIrOptions.DeserializeSupportActionsAsync(
                e => e.Scopes.Contains(SupportScopeEnum.RecordPostProcess) && !string.IsNullOrEmpty(e.AgentName)
                );
            if (filteredActions.Count == 0)
            {
                builder.AddTitle("No post process action found");
            }
            else
            {
                builder.AddTitle("Choose post process action:");

                var noPostProcessAction = new SupportActionJson
                {
                    Name = string.Empty
                };

                builder.AddItems(
                    [
                        ("No post process", string.IsNullOrEmpty(chosenPostProcessActionName), noPostProcessAction)
                    ]
                    );

                builder.AddItems(
                    filteredActions
                    .ConvertAll(a => (a.Name, a.Name == chosenPostProcessActionName, a as object))
                    );
            }


            builder.AddTitle("Other:");
            builder.AddItems(
                [
                    ("Recording enabled", RecordingPage.Instance.Enabled, RecordingOtherActionEnum.EnableDisable as object)
                ]
                );
            builder.AddItems(
                [
                    ("Show help", false, RecordingOtherActionEnum.ShowHelp as object)
                ]
                );


            var chosenMenuItem = await builder.ShowAsync<object>();

            return chosenMenuItem;
        }

        /// <summary>
        /// Discovers every MEF-exported <see cref="IRecorderFactory"/> so the recorder picker can
        /// list all recording backends registered with Visual Studio's component model.
        /// </summary>
        public static async Task<List<IRecorderFactory>> ObtainRecorderFactoriesAsync()
        {
            var componentModel = await MefHelper.GetComponentModelAsync();
            var recorderFactories = componentModel.DefaultExportProvider.GetExports<IRecorderFactory>()
                .Select(l => l.Value)
                .ToList()
                ;
            return recorderFactories;
        }

    }

    /// <summary>
    /// The non-recorder, non-post-process entries in the recording context menu: toggling whether
    /// recording is enabled, or opening the recording feature's help.
    /// </summary>
    public enum RecordingOtherActionEnum
    {
        /// <summary>Toggles <see cref="RecordingPage.Enabled"/>, turning voice recording on or off.</summary>
        EnableDisable,
        /// <summary>Opens the help for the recording feature.</summary>
        ShowHelp
    }
}
