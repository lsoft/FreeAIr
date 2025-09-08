using FreeAIr.Options2;
using FreeAIr.Options2.Support;
using FreeAIr.Record;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FreeAIr.UI.ContextMenu
{
    public static class RecorderContextMenu
    {
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

        public static async Task<List<IRecorderFactory>> ObtainRecorderFactoriesAsync()
        {
            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
            var recorderFactories = componentModel.DefaultExportProvider.GetExports<IRecorderFactory>()
                .Select(l => l.Value)
                .ToList()
                ;
            return recorderFactories;
        }

    }

    public enum RecordingOtherActionEnum
    {
        EnableDisable,
        ShowHelp
    }
}
