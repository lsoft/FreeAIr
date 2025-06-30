using FreeAIr.Agents;
using FreeAIr.Embedding;
using FreeAIr.Git;
using FreeAIr.Git.Parser;
using FreeAIr.Helper;
using FreeAIr.NLOutline.Tree.Builder;
using FreeAIr.NLOutline.Tree.Builder.File;
using FreeAIr.Shared.Helper;
using FreeAIr.UI.NestedCheckBox;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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
        private ICommand _openJsonFolderCommand;
        private ICommand _updateJsonFileCommand;

        public string JsonFilePath
        {
            get => _jsonFilePath;
            private set
            {
                _jsonFilePath = value;
                OnPropertyChanged(nameof(JsonFilePath));
            }
        }

        public ICommand OpenJsonFolderCommand
        {
            get
            {
                if (_openJsonFolderCommand is null)
                {
                    _openJsonFolderCommand = new RelayCommand(
                        a =>
                        {
                            var fi = new FileInfo(_jsonFilePath);
                            var folderPath = fi.Directory.FullName;
                            if (!Directory.Exists(folderPath))
                            {
                                VS.MessageBox.Show("Folder does not exists yet. Save NLO Json file to create the folder.");
                            }

                            Process.Start("explorer.exe", folderPath);
                        }
                        );
                }

                return _openJsonFolderCommand;
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

        public ObservableCollection2<AgentWrapper> GenerateNLOAgentList
        {
            get;
        }

        public AgentWrapper SelectedGenerateNLOAgent
        {
            get;
            set;
        }

        public ObservableCollection2<AgentWrapper> GenerateEmbeddingAgentList
        {
            get;
        }

        public AgentWrapper SelectedGenerateEmbeddingAgent
        {
            get;
            set;
        }



        public string UpdateJsonFileCommandContent
        {
            get
            {
                if (_completeRebuild)
                {
                    return "(Re)write NLO json file completely...";
                }

                return "Update NLO json file...";
            }
        }

        public ICommand UpdateJsonFileCommand
        {
            get
            {
                if (_updateJsonFileCommand is null)
                {
                    _updateJsonFileCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            try
                            {
                                var outputPane = await VS.Windows.CreateOutputWindowPaneAsync("FreeAIr NLO Json file generation");
                                await outputPane.ActivateAsync();
                                await outputPane.WriteLineAsync();
                                await outputPane.WriteLineAsync(new string('-', 80));
                                await outputPane.WriteLineAsync(DateTime.Now.ToString());

                                try
                                {
                                    if (_completeRebuild)
                                    {
                                        await outputPane.WriteLineAsync("Starting complete rebuild...");

                                        var embeddingContainer = await TreeBuilder.BuildAsync(
                                            SelectedGenerateNLOAgent.Agent,
                                            CancellationToken.None
                                            );

                                        var eg = new EmbeddingGenerator(SelectedGenerateEmbeddingAgent.Agent);
                                        await eg.GenerateEmbeddingsAsync(embeddingContainer);

                                        var jsonEmbeddingFilePath = await EmbeddingContainer.GenerateFilePathAsync();

                                        await embeddingContainer.SerializeAsync(
                                            jsonEmbeddingFilePath
                                            );
                                    }
                                    else
                                    {
                                        await outputPane.WriteLineAsync("Starting update...");

                                        await VS.MessageBox.ShowAsync("Not implemented");
                                    }

                                    await outputPane.WriteLineAsync("Process SUCESSFULLY completed.");
                                    await outputPane.HideAsync();
                                }
                                catch (Exception excp)
                                {
                                    await outputPane.WriteLineAsync("Error:");
                                    await outputPane.WriteLineAsync(excp.Message);
                                    await outputPane.WriteLineAsync(excp.StackTrace);
                                    throw;
                                }
                            }
                            catch (Exception excp)
                            {
                                await VS.MessageBox.ShowErrorAsync(
                                    $"Error: {excp.Message}"
                                    + Environment.NewLine
                                    + excp.StackTrace
                                    );
                            }
                        },
                        a =>
                        {
                            if (SelectedGenerateNLOAgent is null)
                            {
                                return false;
                            }
                            if (SelectedGenerateEmbeddingAgent is null)
                            {
                                return false;
                            }

                            return true;
                        });
                }

                return _updateJsonFileCommand;
            }
        }

        public BuildNaturalLanguageOutlinesJsonFileToolViewModel(
            )
        {
            Groups = new ObservableCollection2<CheckableItem>();
            GenerateNLOAgentList = new ObservableCollection2<AgentWrapper>();
            GenerateEmbeddingAgentList = new ObservableCollection2<AgentWrapper>();
        }

        public async Task UpdatePageAsync()
        {
            JsonFilePath = await EmbeddingContainer.GenerateFilePathAsync();

            FillAgents();

            if (_completeRebuild)
            {
                FillGroupsFromSolution();
            }
            else
            {
                await FillGroupsFromGitChangesAsync();
            }

            OnPropertyChanged();
        }

        private void FillAgents()
        {
            GenerateNLOAgentList.Clear();
            GenerateNLOAgentList.AddRange(
                InternalPage.Instance.GetAgentCollection().Agents
                    .ConvertAll(a => new AgentWrapper(a))
                    );
            SelectedGenerateNLOAgent = GenerateNLOAgentList.FirstOrDefault();

            GenerateEmbeddingAgentList.Clear();
            GenerateEmbeddingAgentList.AddRange(
                InternalPage.Instance.GetAgentCollection().Agents
                    .ConvertAll(a => new AgentWrapper(a))
                    );
            SelectedGenerateEmbeddingAgent = GenerateEmbeddingAgentList.FirstOrDefault();
        }

        private async Task FillGroupsFromGitChangesAsync()
        {
            var diff = await GitDiffCreator.BuildGitDiffAsync();

            Dictionary<string, GitDiffFile> addedFiles = diff.Files
                .FindAll(f => f.Status == GitDiffFileStatusEnum.Added)
                .ToDictionary(f => f.NewFullPath, f => f)
                ;
            Dictionary<string, GitDiffFile> updatedFiles = diff.Files
                .FindAll(f => f.Status == GitDiffFileStatusEnum.Updated)
                .ToDictionary(f => f.OriginalFullPath, f => f)
                ;
            Dictionary<string, GitDiffFile> deletedFiles = diff.Files
                .FindAll(f => f.Status == GitDiffFileStatusEnum.Deleted)
                .ToDictionary(f => f.OriginalFullPath, f => f)
                ;

            FillGroupsFromSolution(
                addedFiles,
                updatedFiles,
                deletedFiles
                );
        }

        private void FillGroupsFromSolution(
            Dictionary<string, GitDiffFile>? addedFiles = null,
            Dictionary<string, GitDiffFile>? updatedFiles = null,
            Dictionary<string, GitDiffFile>? deletedFiles = null
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

                    var fullPath = item.FullPath ?? item.Name;

                    Brush? foreground = null;
                    if (addedFiles is not null && addedFiles.ContainsKey(fullPath))
                    {
                        foreground = Brushes.Green;
                    }
                    if (deletedFiles is not null && deletedFiles.ContainsKey(fullPath))
                    {
                        foreground = Brushes.Red;
                    }

                    Brush? disabledForeground = null;
                    var isChecked = true;
                    if (addedFiles is not null && !addedFiles.ContainsKey(fullPath))
                    {
                        if (updatedFiles is not null && !updatedFiles.ContainsKey(fullPath))
                        {
                            if (deletedFiles is not null && !deletedFiles.ContainsKey(fullPath))
                            {
                                disabledForeground = Brushes.DarkGray;
                                isChecked = false;
                            }
                        }
                    }

                    return new CheckableItem(
                        itemName,
                        string.Empty, //item.FullPath ?? itemName,
                        isChecked,
                        new CheckableItemStyle(
                            foreground,
                            disabledForeground
                            ),
                        item
                        );
                },
                (root, child) => root.AddChild(child),
                CancellationToken.None
                );

            if (deletedFiles is not null)
            {
                foreach (var pair in deletedFiles)
                {
                    var filePath = pair.Key;
                    var fileInfo = new FileInfo(filePath);

                    root.AddChild(
                        new CheckableItem(
                            fileInfo.Name,
                            filePath,
                            true,
                            new CheckableItemStyle(
                                Brushes.Red,
                                null,
                                TextDecorations.Strikethrough
                                ),
                            filePath
                            )
                        );
                }
            }

            Groups.Clear();
            Groups.Add(root);
        }
    }

    public sealed class AgentWrapper : BaseViewModel
    {
        public Agent Agent
        {
            get;
        }

        public string AgentName => Agent.Name;

        public string Technical => $"{Agent.Technical.ChosenModel} ({Agent.Technical.Endpoint})";

        public AgentWrapper(
            Agent agent
            )
        {
            if (agent is null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            Agent = agent;
        }

    }
}
