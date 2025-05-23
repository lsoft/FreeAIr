using FreeAIr.Shared.Helper;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.BuildErrors
{
    public static class BuildResultProvider
    {
        public static async Task<List<BuildResultInformation>> GetBuildResultInformationsAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var result = new List<BuildResultInformation>();

            if (await FreeAIrPackage.Instance.GetServiceAsync(typeof(SVsErrorList)) is not IVsTaskList tasks)
            {
                return result;
            }

            int invocationResult = VSConstants.S_OK;

            tasks.EnumTaskItems(out IVsEnumTaskItems itemsEnum);

            var vsTaskItem = new IVsTaskItem[1];
            while (itemsEnum.Next(1, vsTaskItem, null) == 0)
            {
                var taskItem = vsTaskItem[0];
                var errorTaskItem = taskItem as IVsErrorItem;

                invocationResult |= taskItem.get_Text(out var text);
                invocationResult |= taskItem.Document(out string document);
                invocationResult |= taskItem.Line(out var line);
                invocationResult |= taskItem.Column(out var column);
                invocationResult |= errorTaskItem.GetCategory(out uint category);
                if (invocationResult != VSConstants.S_OK)
                {
                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        "Cannot obtain error information."
                        );
                    return new List<BuildResultInformation>();
                }

                var isError = category == (uint)TaskErrorCategory.Error;
                var isWarning = category == (uint)TaskErrorCategory.Warning;

                var errorInformation = new BuildResultInformation(
                    isError
                        ? ErrorInformationTypeEnum.Error
                        : isWarning
                            ? ErrorInformationTypeEnum.Warning
                            : ErrorInformationTypeEnum.Information,
                    document,
                    text,
                    line,
                    column
                    );
                result.Add(errorInformation);
            }

            return result;
        }

        public static async Task<BuildResultInformation?> GetSelectedErrorInformationAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (await FreeAIrPackage.Instance.GetServiceAsync(typeof(SVsErrorList)) is not IVsTaskList2 tasks)
            {
                return null;
            }

            tasks.EnumSelectedItems(out IVsEnumTaskItems itemsEnum);

            var vsTaskItem = new IVsTaskItem[1];

            if (itemsEnum.Next(1, vsTaskItem, null) == 0)
            {
                var taskItem = vsTaskItem[0];
                var errorTaskItem = taskItem as IVsErrorItem;
                if (errorTaskItem is null)
                {
                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        "Cannot obtain error information."
                        );
                    return null;
                }

                var categoryResult = errorTaskItem.GetCategory(out uint category);
                if (categoryResult != VSConstants.S_OK)
                {
                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        "Cannot obtain error information."
                        );
                    return null;
                }
                if (category.NotIn((uint)TaskErrorCategory.Error))
                {
                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        "FreeAIr is supposed to fix only errors. If you want analyze build warning, file an issue to its dev."
                        );
                    return null;
                }

                var documentResult = taskItem.Document(out string document);
                if (documentResult != VSConstants.S_OK || string.IsNullOrEmpty(document))
                {
                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        "Cannot obtain error information."
                        );
                    return null;
                }

                var lineResult = taskItem.Line(out int line);
                if (lineResult != VSConstants.S_OK)
                {
                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        "Cannot obtain error information."
                        );
                    return null;
                }

                var columnResult = taskItem.Column(out int column);
                if (columnResult != VSConstants.S_OK)
                {
                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        "Cannot obtain error information."
                        );
                    return null;
                }

                var getTextResult = taskItem.get_Text(out string description);
                if (getTextResult != VSConstants.S_OK || string.IsNullOrEmpty(description))
                {
                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        "Cannot obtain error information."
                        );
                    return null;
                }

                return new BuildResultInformation(
                    ErrorInformationTypeEnum.Error,
                    document,
                    description,
                    line,
                    column
                    );

            }

            return null;
        }
    }
}
