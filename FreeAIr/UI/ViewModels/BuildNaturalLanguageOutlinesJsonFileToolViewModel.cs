using FreeAIr.Embedding;
using FreeAIr.Git;
using FreeAIr.Helper;
using FreeAIr.NLOutline.Tree.Builder.File;
using FreeAIr.Shared.Helper;
using FreeAIr.UI.NestedCheckBox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    [Export(typeof(BuildNaturalLanguageOutlinesJsonFileToolViewModel))]
    public sealed class BuildNaturalLanguageOutlinesJsonFileToolViewModel : BaseViewModel
    {
        private string _jsonFilePath;
        private ICommand _reloadPageCommand;
        private bool _completeRebuild;

        public string JsonFilePath
        {
            get => _jsonFilePath;
            private set
            {
                _jsonFilePath = value;
                OnPropertyChanged(nameof(JsonFilePath));
            }
        }

        public ICommand ReloadPageCommand
        {
            get
            {
                if (_reloadPageCommand is null)
                {
                    _reloadPageCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            await UpdatePageAsync();
                        });
                }

                return _reloadPageCommand;
            }
        }

        public bool CompleteRebuild
        {
            get => _completeRebuild;
            set
            {
                _completeRebuild = value;

                UpdatePageAsync()
                    .FileAndForget(nameof(UpdatePageAsync));
            }
        }

        public ObservableCollection2<CheckableItem> Groups
        {
            get;
        }

        public ObservableCollection2<ScannerWrapper> NLOScannerList
        {
            get;
        }

        public ScannerWrapper SelectedScanner
        {
            get;
            set;
        }

        public BuildNaturalLanguageOutlinesJsonFileToolViewModel(
            )
        {
            Groups = new ObservableCollection2<CheckableItem>();
            NLOScannerList = new ObservableCollection2<ScannerWrapper>();
        }

        public async Task UpdatePageAsync()
        {
            JsonFilePath = await EmbeddingContainer.GenerateFilePathAsync();

            if (_completeRebuild)
            {
                FillGroupsFromSolution();
            }
            else
            {
                await FillGroupsFromGitChangesAsync();
            }

            FillScanners();

            OnPropertyChanged();
        }

        private void FillScanners()
        {
            NLOScannerList.Clear();

            SelectedScanner = new ScannerWrapper(FileScanner.DefaultInstance);

            NLOScannerList.Add(
                SelectedScanner
                );

            NLOScannerList.AddRange(
                FileScannerFactory.Scanners.ConvertAll(s => new ScannerWrapper(s))
                );
        }
        private async Task FillGroupsFromGitChangesAsync()
        {
            return;
            var diff = await GitDiffCreator.BuildGitDiffAsync();

            var a = diff.Files
                .FindAll(f => f.Status == Git.Parser.GitDiffFileStatusEnum.Added)
                ;
            var u = diff.Files
                .FindAll(f => f.Status == Git.Parser.GitDiffFileStatusEnum.Updated)
                ;
            var d = diff.Files
                .FindAll(f => f.Status == Git.Parser.GitDiffFileStatusEnum.Deleted)
                ;


            var addedFiles = diff.Files
                .FindAll(f => f.Status == Git.Parser.GitDiffFileStatusEnum.Added)
                .ToDictionary(f => f.NewFullPath, f => f)
                ;
            var updatedFiles = diff.Files
                .FindAll(f => f.Status == Git.Parser.GitDiffFileStatusEnum.Updated)
                .ToDictionary(f => f.OriginalFullPath, f => f)
                ;
            var deletedFiles = diff.Files
                .FindAll(f => f.Status == Git.Parser.GitDiffFileStatusEnum.Deleted)
                .ToDictionary(f => f.OriginalFullPath, f => f)
                ;



        }

        private void FillGroupsFromSolution(
            )
        {
            var solution = VS.Solutions.GetCurrentSolution();
            var root = solution.ConvertRecursivelyFor<CheckableItem>(
                item =>
                {
                    if (item.Type.NotIn(SolutionItemType.SolutionFolder, SolutionItemType.Solution, SolutionItemType.Project, SolutionItemType.PhysicalFolder, SolutionItemType.PhysicalFile))
                    {
                        return null;
                    }

                    var itemName = item.Name;
                    if (item.Type.In(SolutionItemType.PhysicalFile))
                    {
                        var fi = new FileInfo(item.FullPath);
                        itemName = fi.Name;
                    }
                    if (item.Type.In(SolutionItemType.PhysicalFolder))
                    {
                        var fi = new DirectoryInfo(item.FullPath);
                        itemName = fi.Name;
                    }

                    return new CheckableItem(
                        itemName,
                        string.Empty, //item.FullPath ?? itemName,
                        true,
                        Brushes.Red,
                        item
                        );
                },
                (root, child) => root.AddChild(child),
                CancellationToken.None
                );

            Groups.Clear();
            Groups.Add(root);
        }
    }

    public sealed class ScannerWrapper : BaseViewModel
    {
        public IFileScanner Scanner
        {
            get;
        }

        public string ScannerName => Scanner.Name;

        public string ScannerDescription => Scanner.Description;

        public ScannerWrapper(
            IFileScanner scanner
            )
        {
            Scanner = scanner;
        }
    }
}
