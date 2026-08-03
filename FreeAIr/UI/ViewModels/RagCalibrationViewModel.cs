using FreeAIr.Embedding;
using FreeAIr.Embedding.Json;
using FreeAIr.Find;
using FreeAIr.Helper;
using FreeAIr.Options2;
using FreeAIr.Options2.Agent;
using FreeAIr.Options2.Rag;
using FreeAIr.UI.ContextMenu;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Threading;
//`Task<T>` needs this: FreeAIrPackage.cs declares a global `Task` alias which shadows the generic one
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    /// <summary>
    /// The window where the threshold of the `Use RAG` search is measured instead of guessed.
    ///
    /// The user asks the index the questions they would ask the search and says, per question,
    /// which file answers it — or that nothing here does. Both answers are worth the same to the
    /// measurement: the hopeless ones say how high pure noise climbs on this model, the labelled
    /// ones stop the threshold from climbing over an answer the user knows is right. The questions
    /// go into the settings and are reused by every later index build.
    ///
    /// Everything happens against the index which is already on disk, and a recalculation rewrites
    /// its metadata file alone. Rebuilding the vectors costs an hour of a model's time and would
    /// produce the very same vectors, so this window never asks for one.
    /// </summary>
    [Export(typeof(RagCalibrationViewModel))]
    public sealed class RagCalibrationViewModel : BaseViewModel
    {
        /// <summary>
        /// How deep the probe looks. Deliberately more than a search would show: the file the user
        /// is looking for is interesting exactly when it did not make the cut.
        /// </summary>
        private const int _probeFileCount = 20;

        private readonly SemaphoreSlim _locker = new SemaphoreSlim(1, 1);

        private CancellationTokenSource? _cancellationTokenSource;

        private EmbeddingIndex? _index;
        private AgentJson? _embeddingAgent;

        /// <summary>
        /// The agent the index says built it, kept even when the settings no longer have an agent
        /// under that name: the name itself is what tells the user which one to look for.
        /// </summary>
        private string? _indexAgentName;

        /// <summary>
        /// Whether the model behind <see cref="_embeddingAgent"/> is the one which built the index,
        /// asked once per loaded index: the answer cannot change while both stay the same.
        /// </summary>
        private bool? _sameSpace;

        /// <summary>
        /// The result of the last recalculation. Only this may be written into the index — the
        /// numbers on the screen have to be the numbers which get stored.
        /// </summary>
        private EmbeddingCalibration? _measured;

        private string _status = string.Empty;
        private string _indexDescription = string.Empty;
        private string _agentDescription = string.Empty;
        private string _calibrationDescription = string.Empty;
        private string _warning = string.Empty;
        private Visibility _warningVisibility = Visibility.Collapsed;
        private string _query = string.Empty;
        private double _sensitivity = 0.2d;
        private bool _isBusy;

        public string Status
        {
            get => _status;
            private set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public string IndexDescription
        {
            get => _indexDescription;
            private set
            {
                _indexDescription = value;
                OnPropertyChanged(nameof(IndexDescription));
            }
        }

        /// <summary>
        /// Which model everything in this window is measured with. Shown rather than assumed: the
        /// numbers are a property of that model, and every failure to reach it — a wrong endpoint,
        /// a model name the server does not know — reads as a failure of the window until the user
        /// can see which agent it is talking to.
        /// </summary>
        public string AgentDescription
        {
            get => _agentDescription;
            private set
            {
                _agentDescription = value;
                OnPropertyChanged(nameof(AgentDescription));
            }
        }

        public string CalibrationDescription
        {
            get => _calibrationDescription;
            private set
            {
                _calibrationDescription = value;
                OnPropertyChanged(nameof(CalibrationDescription));
            }
        }

        /// <summary>
        /// The one outcome no setting can rescue, kept apart from the numbers so that it cannot be
        /// read as one of them.
        /// </summary>
        public string Warning
        {
            get => _warning;
            private set
            {
                _warning = value;
                OnPropertyChanged(nameof(Warning));
            }
        }

        public Visibility WarningVisibility
        {
            get => _warningVisibility;
            private set
            {
                _warningVisibility = value;
                OnPropertyChanged(nameof(WarningVisibility));
            }
        }

        public string Query
        {
            get => _query;
            set
            {
                _query = value;
                OnPropertyChanged(nameof(Query));
            }
        }

        /// <summary>
        /// The setting the whole window exists to choose, edited here and saved with the queries.
        /// </summary>
        public double Sensitivity
        {
            get => _sensitivity;
            set
            {
                _sensitivity = value;
                OnPropertyChanged(nameof(Sensitivity));

                //the same measurement, read at another sensitivity: no reason to ask the model again
                ShowCalibration(_measured ?? _index?.Calibration);
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                _isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
            }
        }

        public Visibility ProgressVisibility => _isBusy
            ? Visibility.Visible
            : Visibility.Collapsed
            ;

        /// <summary>
        /// What the last query found, threshold included but not applied: the rows below the
        /// threshold are the ones which tell whether it sits too high.
        /// </summary>
        public ObservableCollection2<RagProbeResultViewModel> Results
        {
            get;
        }

        /// <summary>
        /// The queries the threshold is measured with, as they will be written into the settings.
        /// </summary>
        public ObservableCollection2<RagProbeViewModel> Probes
        {
            get;
        }

        public ICommand ReloadCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(
                        async a =>
                        {
                            await ReloadAsync();
                        },
                        a => !IsBusy
                        );
                }

                return field;
            }
        }

        /// <summary>
        /// Picks another agent by hand. Needed whenever the index names one the settings no longer
        /// have, and whenever the named one turns out not to answer — an agent pointing at a chat
        /// model, or at an endpoint with no embeddings at all, fails with a flat HTTP error and
        /// there would otherwise be no way to try a different one.
        /// </summary>
        public ICommand ChangeAgentCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(
                        async a =>
                        {
                            await ChangeAgentAsync();
                        },
                        a => !IsBusy
                        );
                }

                return field;
            }
        }

        public ICommand ProbeCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(
                        async a =>
                        {
                            await ProbeAsync();
                        },
                        a => !IsBusy && !string.IsNullOrWhiteSpace(Query)
                        );
                }

                return field;
            }
        }

        /// <summary>
        /// "This file answers my query". The query and the file become a labelled probe, which is
        /// what keeps the threshold from climbing above an answer the user knows to be right.
        /// </summary>
        public ICommand MarkAsAnswerCommand
        {
            get
            {
                if (field is null)
                {
                    field = new RelayCommand(
                        a =>
                        {
                            var result = a as RagProbeResultViewModel;
                            if (result is null)
                            {
                                return;
                            }

                            AddProbe(Query, result.RelativePath);
                        },
                        a => a is RagProbeResultViewModel && !string.IsNullOrWhiteSpace(Query)
                        );
                }

                return field;
            }
        }

        /// <summary>
        /// "Nothing here answers my query". The more plausible such a query is for this codebase,
        /// the more honest the threshold measured from it.
        /// </summary>
        public ICommand MarkAsHopelessCommand
        {
            get
            {
                if (field is null)
                {
                    field = new RelayCommand(
                        a =>
                        {
                            AddProbe(Query, string.Empty);
                        },
                        a => !string.IsNullOrWhiteSpace(Query)
                        );
                }

                return field;
            }
        }

        public ICommand RemoveProbeCommand
        {
            get
            {
                if (field is null)
                {
                    field = new RelayCommand(
                        a =>
                        {
                            var probe = a as RagProbeViewModel;
                            if (probe is null)
                            {
                                return;
                            }

                            Probes.Remove(probe);
                            OnPropertyChanged();
                        },
                        a => a is RagProbeViewModel
                        );
                }

                return field;
            }
        }

        public ICommand RecalculateCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(
                        async a =>
                        {
                            await RecalculateAsync();
                        },
                        a => !IsBusy
                        );
                }

                return field;
            }
        }

        public ICommand SaveCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(
                        async a =>
                        {
                            await SaveAsync();
                        },
                        a => !IsBusy
                        );
                }

                return field;
            }
        }

        public ICommand CancelCommand
        {
            get
            {
                if (field is null)
                {
                    field = new RelayCommand(
                        a =>
                        {
                            _cancellationTokenSource?.Cancel();
                        },
                        a => IsBusy
                        );
                }

                return field;
            }
        }

        public RagCalibrationViewModel()
        {
            Results = new ObservableCollection2<RagProbeResultViewModel>();
            Probes = new ObservableCollection2<RagProbeViewModel>();
        }

        /// <summary>
        /// Reads the settings and the index. Everything else in the window works against what this
        /// has loaded, so a failure here leaves the window empty rather than half usable.
        /// </summary>
        public async Task ReloadAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            _index = null;
            _sameSpace = null;
            _measured = null;
            _embeddingAgent = null;
            _indexAgentName = null;

            Results.Clear();
            Probes.Clear();
            IndexDescription = string.Empty;
            CalibrationDescription = string.Empty;
            AgentDescription = string.Empty;
            HideWarning();

            if (!SolutionHelper.TryGetSolution(out _))
            {
                Status = FreeAIr.Resources.Resources.RAG__there_is_no_index;
                OnPropertyChanged();
                return;
            }

            await ResolveAgentQuietlyAsync();

            var rag = await FreeAIrOptions.DeserializeRagAsync();

            _sensitivity = rag.Sensitivity;
            OnPropertyChanged(nameof(Sensitivity));

            foreach (var probe in rag.Calibration.Relevant)
            {
                if (string.IsNullOrWhiteSpace(probe.Query))
                {
                    continue;
                }

                Probes.Add(new RagProbeViewModel(probe.Query, probe.ExpectedPath));
            }

            foreach (var probe in rag.Calibration.Irrelevant)
            {
                if (string.IsNullOrWhiteSpace(probe))
                {
                    continue;
                }

                Probes.Add(new RagProbeViewModel(probe, string.Empty));
            }

            await WithBusyAsync(
                FreeAIr.Resources.Resources.RAG__preparing_the_index,
                async cancellationToken =>
                {
                    var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
                    var container = componentModel.GetService<EmbeddingIndexContainer>();

                    var index = await container.GetAsync(null, cancellationToken);

                    //GetAsync leaves us on a background thread, and everything below is bound to
                    //the window
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                    _index = index;

                    if (index is null)
                    {
                        Status = FreeAIr.Resources.Resources.RAG__there_is_no_index;
                        return;
                    }

                    IndexDescription = string.Format(
                        FreeAIr.Resources.Resources.RAG_calibration__index_summary,
                        index.GenerateDateTime.ToString("g"),
                        index.EmbeddingModel ?? "?",
                        index.ReportedEmbeddingModel ?? "?",
                        index.Entries.Count,
                        index.Dimensions
                        );

                    ShowCalibration(index.Calibration);

                    Status = string.Empty;
                });

            OnPropertyChanged();
        }

        private async Task ProbeAsync()
        {
            var query = Query?.Trim();
            if (string.IsNullOrEmpty(query))
            {
                return;
            }

            await WithBusyAsync(
                FreeAIr.Resources.Resources.RAG__vectorizing_the_query,
                async cancellationToken =>
                {
                    var index = await RequireIndexAsync(cancellationToken);
                    if (index is null)
                    {
                        return;
                    }

                    var vectorizer = await RequireVectorizerAsync();
                    if (vectorizer is null)
                    {
                        return;
                    }

                    var shortlist = await RagShortlist.ProbeAsync(
                        index,
                        vectorizer,
                        query!,
                        new RagShortlistOptions
                        {
                            TopOutlineCount = 200,
                            MaxFileCount = _probeFileCount,
                            Sensitivity = Sensitivity
                        },
                        cancellationToken
                        );

                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                    Results.Clear();

                    if (shortlist.SpaceMismatch)
                    {
                        _sameSpace = false;
                        ShowSpaceMismatch(shortlist.SpaceSimilarity!.Value);
                        return;
                    }

                    if (shortlist.SpaceSimilarity.HasValue)
                    {
                        //asked and answered in the very request the query went out in
                        _sameSpace = true;
                    }

                    if (shortlist.ModelMismatch)
                    {
                        Status = string.Format(
                            FreeAIr.Resources.Resources.RAG__the_agent_does_not_match,
                            shortlist.QueryDimensions,
                            shortlist.IndexDimensions
                            );
                        return;
                    }

                    var minScore = shortlist.AppliedMinScore;

                    foreach (var candidate in shortlist.Candidates)
                    {
                        Results.Add(
                            new RagProbeResultViewModel(
                                candidate,
                                minScore
                                )
                            );
                    }

                    Status = Results.Count == 0
                        ? FreeAIr.Resources.Resources.RAG_calibration__nothing_found
                        : string.Format(
                            FreeAIr.Resources.Resources.RAG_calibration__probed__0__files,
                            Results.Count,
                            minScore.ToString("F3")
                            )
                        ;
                });
        }

        /// <summary>
        /// Measures the queries currently on the screen against the index which is loaded. Nothing
        /// is written anywhere: this is the step where the user finds out what their editing did.
        /// </summary>
        private async Task RecalculateAsync()
        {
            await WithBusyAsync(
                FreeAIr.Resources.Resources.RAG_calibration__recalculating,
                async cancellationToken =>
                {
                    var index = await RequireIndexAsync(cancellationToken);
                    if (index is null)
                    {
                        return;
                    }

                    var vectorizer = await RequireVectorizerAsync();
                    if (vectorizer is null)
                    {
                        return;
                    }

                    if (!await EnsureSameSpaceAsync(index, vectorizer, cancellationToken))
                    {
                        return;
                    }

                    var calibration = await RagCalibrator.RunAsync(
                        index,
                        vectorizer,
                        BuildProbes(),
                        cancellationToken
                        );

                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                    _measured = calibration;

                    ShowCalibration(calibration);

                    Status = calibration is null
                        ? FreeAIr.Resources.Resources.RAG_calibration__nothing_to_measure
                        : FreeAIr.Resources.Resources.RAG_calibration__recalculated
                        ;

                    //the scores on the screen were painted against the old threshold
                    foreach (var result in Results)
                    {
                        result.ApplyThreshold(
                            calibration?.ComputeThreshold(Sensitivity) ?? 0f
                            );
                    }
                });
        }

        /// <summary>
        /// The queries go into the settings, the numbers into the index. Two files, because the two
        /// things have different lifetimes: the queries survive every rebuild of the index, while
        /// the numbers belong to the vectors they were measured against.
        /// </summary>
        private async Task SaveAsync()
        {
            await WithBusyAsync(
                FreeAIr.Resources.Resources.RAG_calibration__saving,
                async cancellationToken =>
                {
                    var options = await FreeAIrOptions.DeserializeAsync(null);

                    options.Rag.Sensitivity = Sensitivity;
                    options.Rag.Calibration = BuildSettingsNode();

                    var place = await options.SerializeAsync(null);

                    var storedIntoIndex = await TryStoreMeasurementAsync(cancellationToken);

                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                    var placeTitle = OptionsPlaceHelper.GetTitle(place);

                    Status = storedIntoIndex
                        ? string.Format(
                            FreeAIr.Resources.Resources.RAG_calibration__saved__0_,
                            placeTitle
                            )
                        : string.Format(
                            FreeAIr.Resources.Resources.RAG_calibration__saved_settings_only__0_,
                            placeTitle
                            )
                        ;
                });
        }

        /// <summary>
        /// Writes the measured numbers into the metadata file of the index, so that the search
        /// starts using them without a rebuild. Only a fresh measurement is stored — writing the
        /// numbers of the previous model, or numbers nobody has seen, would be worse than leaving
        /// the file alone.
        /// </summary>
        private async Task<bool> TryStoreMeasurementAsync(
            CancellationToken cancellationToken
            )
        {
            var measured = _measured;
            if (measured is null)
            {
                return false;
            }

            //resolved from the solution rather than from the loaded index: a save which follows
            //another save has already dropped the loaded one, and it still has to write
            var filePath = await FreeAIrOptions.ComposeEmbeddingsFilePathAsync();
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
            {
                return false;
            }

            //read back rather than reuse what is in memory: the metadata file holds fields this
            //window knows nothing about, and they all have to survive the write
            var json = await EmbeddingOutlineJsonObject.DeserializeAsync(
                filePath!,
                false,
                cancellationToken
                );
            if (json is null)
            {
                return false;
            }

            json.Calibration = new CalibrationJsonObject(measured);

            await json.SerializeMetadataAsync(
                filePath!,
                cancellationToken
                );

            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
            componentModel.GetService<EmbeddingIndexContainer>().Invalidate();

            //the index in this window is now the stale one
            _index = null;

            return true;
        }

        private RagCalibrationJson BuildSettingsNode()
        {
            var result = new RagCalibrationJson();

            foreach (var probe in Probes)
            {
                var query = probe.Query?.Trim();
                if (string.IsNullOrEmpty(query))
                {
                    continue;
                }

                var path = probe.ExpectedPath?.Trim();
                if (string.IsNullOrEmpty(path))
                {
                    result.Irrelevant.Add(query!);
                    continue;
                }

                result.Relevant.Add(
                    new RagRelevantProbeJson
                    {
                        Query = query!,
                        ExpectedPath = path!
                    }
                    );
            }

            return result;
        }

        private RagCalibrationProbes BuildProbes()
        {
            var node = BuildSettingsNode();

            return AgentEmbedding.CreateCalibrationProbes(
                new RagJson
                {
                    Calibration = node
                }
                );
        }

        private void AddProbe(
            string query,
            string expectedPath
            )
        {
            query = query?.Trim() ?? string.Empty;
            if (query.Length == 0)
            {
                return;
            }

            foreach (var existing in Probes)
            {
                if (!string.Equals(existing.Query?.Trim(), query, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                //the same question answered twice: the last answer is the one the user means
                existing.ExpectedPath = expectedPath;
                Status = FreeAIr.Resources.Resources.RAG_calibration__query_replaced;
                OnPropertyChanged();
                return;
            }

            Probes.Add(new RagProbeViewModel(query, expectedPath));

            Status = FreeAIr.Resources.Resources.RAG_calibration__query_added;
            OnPropertyChanged();
        }

        private void ShowCalibration(
            EmbeddingCalibration? calibration
            )
        {
            if (calibration is null)
            {
                CalibrationDescription = FreeAIr.Resources.Resources.RAG_calibration__not_calibrated;
                HideWarning();
                return;
            }

            CalibrationDescription = string.Format(
                FreeAIr.Resources.Resources.RAG_calibration__summary,
                calibration.NoiseCeiling.ToString("F3"),
                calibration.ComputeThreshold(Sensitivity).ToString("F3"),
                Sensitivity.ToString("F2", CultureInfo.CurrentCulture),
                calibration.IrrelevantProbeCount,
                calibration.RelevantProbeCount
                );

            if (!calibration.ModelSeparates)
            {
                ShowWarning(
                    string.Format(
                        FreeAIr.Resources.Resources.RAG__calibration_model_does_not_separate,
                        calibration.RelevantFloor.ToString("F3"),
                        calibration.NoiseCeiling.ToString("F3")
                        )
                    );
                return;
            }

            if (calibration.RelevantMissCount > 0)
            {
                ShowWarning(
                    string.Format(
                        FreeAIr.Resources.Resources.RAG__calibration__0__of__1__probes_missed,
                        calibration.RelevantMissCount,
                        calibration.RelevantProbeCount
                        )
                    );
                return;
            }

            HideWarning();
        }

        /// <summary>
        /// Verifies that the model in hand is the one which built the index, once per pair. A
        /// measurement taken with another model is not a bad measurement, it is a meaningless one.
        /// </summary>
        private async Task<bool> EnsureSameSpaceAsync(
            EmbeddingIndex index,
            IEmbeddingVectorizer vectorizer,
            CancellationToken cancellationToken
            )
        {
            if (_sameSpace.HasValue)
            {
                if (!_sameSpace.Value)
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                    ShowSpaceMismatch(null);
                }

                return _sameSpace.Value;
            }

            if (index.Fingerprint is null)
            {
                //an index built before fingerprints existed: nothing to compare against, and
                //refusing to calibrate it would leave the user without a way to fix it
                _sameSpace = true;
                return true;
            }

            var fingerprint = await EmbeddingSpaceFingerprint.BuildAsync(
                vectorizer,
                cancellationToken
                );
            if (fingerprint is null)
            {
                _sameSpace = true;
                return true;
            }

            var similarity = index.Fingerprint.SimilarityTo(fingerprint.Vectors);

            _sameSpace = similarity >= EmbeddingSpaceFingerprint.SameSpaceThreshold;

            if (!_sameSpace.Value)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                ShowSpaceMismatch(similarity);
            }

            return _sameSpace.Value;
        }

        private void ShowSpaceMismatch(
            float? similarity
            )
        {
            var message = string.Format(
                FreeAIr.Resources.Resources.RAG__another_model_built_the_index__0_,
                similarity.HasValue
                    ? similarity.Value.ToString("F3")
                    : "?"
                );

            Status = message;
            ShowWarning(message);
        }

        private void ShowWarning(
            string message
            )
        {
            Warning = message;
            WarningVisibility = Visibility.Visible;
        }

        private void HideWarning()
        {
            Warning = string.Empty;
            WarningVisibility = Visibility.Collapsed;
        }

        private async Task<EmbeddingIndex?> RequireIndexAsync(
            CancellationToken cancellationToken
            )
        {
            if (_index is not null)
            {
                return _index;
            }

            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
            var container = componentModel.GetService<EmbeddingIndexContainer>();

            var index = await container.GetAsync(null, cancellationToken);

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            _index = index;

            if (index is null)
            {
                Status = FreeAIr.Resources.Resources.RAG__there_is_no_index;
                return null;
            }

            //a reload after a save: the numbers on the screen have to be the ones the search will
            //now be using
            ShowCalibration(index.Calibration);

            return index;
        }

        /// <summary>
        /// The agent whose model built the index, resolved the same way the search resolves it and
        /// then kept: this window asks the model many times in a row, and a picker in front of
        /// every one of them would be unusable.
        /// </summary>
        private async Task<IEmbeddingVectorizer?> RequireVectorizerAsync()
        {
            if (_embeddingAgent is null)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                _embeddingAgent = await DoSearch.DetermineEmbeddingAgentAsync();

                ShowAgent();

                if (_embeddingAgent is null)
                {
                    Status = FreeAIr.Resources.Resources.RAG_calibration__no_agent;
                    return null;
                }
            }

            return AgentEmbedding.CreateVectorizer(_embeddingAgent);
        }

        /// <summary>
        /// Names the agent on the window without asking the user anything. Opening a window is not
        /// an occasion for a modal picker, so an index which names an agent nobody has any more is
        /// reported on the spot and the picker waits for the first request — or for the button.
        /// </summary>
        private async Task ResolveAgentQuietlyAsync()
        {
            var metadata = await EmbeddingIndexContainer.TryReadMetadataAsync();

            _indexAgentName = metadata?.EmbeddingAgentName;

            if (!string.IsNullOrEmpty(_indexAgentName))
            {
                _embeddingAgent = await FreeAIrOptions.DeserializeAgentByNameAsync(
                    _indexAgentName!
                    );
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            ShowAgent();
        }

        private async Task ChangeAgentAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            //no preferred name: the user pressed the button precisely to be asked, and a preference
            //is what the picker skips itself for
            var chosen = await AgentContextMenu.ChooseAnyAgentAsync(
                FreeAIr.Resources.Resources.RAG__choose_the_embedding_agent
                );
            if (chosen is null)
            {
                return;
            }

            _embeddingAgent = chosen;

            //another agent may well be another model, and the answer to `is this the model which
            //built the index` was cached for the previous one
            _sameSpace = null;

            //the scores on the screen were produced by the agent which has just been replaced
            Results.Clear();

            ShowAgent();

            OnPropertyChanged();
        }

        private void ShowAgent()
        {
            if (_embeddingAgent is not null)
            {
                AgentDescription = string.Format(
                    FreeAIr.Resources.Resources.RAG_calibration__agent__0___1___2_,
                    _embeddingAgent.Name,
                    _embeddingAgent.Technical.ChosenModel,
                    _embeddingAgent.Technical.Endpoint
                    );
                return;
            }

            AgentDescription = string.IsNullOrEmpty(_indexAgentName)
                ? FreeAIr.Resources.Resources.RAG_calibration__agent_unknown
                : string.Format(
                    FreeAIr.Resources.Resources.RAG_calibration__agent_not_found__0_,
                    _indexAgentName
                    )
                ;
        }

        /// <summary>
        /// Runs one step of the window: at most one at a time, cancellable, and with every failure
        /// ending up in the status line instead of in a dialog the user cannot act on.
        /// </summary>
        private async Task WithBusyAsync(
            string status,
            Func<CancellationToken, Task> body
            )
        {
            if (!await _locker.WaitAsync(0))
            {
                return;
            }

            var cts = new CancellationTokenSource();
            _cancellationTokenSource = cts;

            IsBusy = true;
            OnPropertyChanged(nameof(ProgressVisibility));
            Status = status;

            try
            {
                await body(cts.Token);
            }
            catch (OperationCanceledException)
            {
                Status = FreeAIr.Resources.Resources.Cancelled;
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();

                Status = FreeAIr.Resources.Resources.Error + $": {excp.Message}";
            }
            finally
            {
                _cancellationTokenSource = null;
                cts.Dispose();

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                IsBusy = false;
                OnPropertyChanged(nameof(ProgressVisibility));
                OnPropertyChanged();

                _locker.Release();
            }
        }
    }

    /// <summary>
    /// One query the threshold is measured with. An empty <see cref="ExpectedPath"/> means the
    /// solution cannot answer this query at all, which is the more valuable of the two kinds.
    /// </summary>
    public sealed class RagProbeViewModel : BaseViewModel
    {
        private string _query;
        private string _expectedPath;

        public string Query
        {
            get => _query;
            set
            {
                _query = value;
                OnPropertyChanged(nameof(Query));
                OnPropertyChanged(nameof(KindDescription));
            }
        }

        public string ExpectedPath
        {
            get => _expectedPath;
            set
            {
                _expectedPath = value;
                OnPropertyChanged(nameof(ExpectedPath));
                OnPropertyChanged(nameof(KindDescription));
            }
        }

        public string KindDescription => string.IsNullOrWhiteSpace(_expectedPath)
            ? FreeAIr.Resources.Resources.RAG_calibration__kind_hopeless
            : FreeAIr.Resources.Resources.RAG_calibration__kind_answered
            ;

        public RagProbeViewModel(
            string query,
            string expectedPath
            )
        {
            _query = query ?? string.Empty;
            _expectedPath = expectedPath ?? string.Empty;
        }
    }

    /// <summary>
    /// One file the probe has found, with the verdict the current threshold gives it. The rows
    /// below the threshold are shown on purpose: a threshold is only judged by what it cuts.
    /// </summary>
    public sealed class RagProbeResultViewModel : BaseViewModel
    {
        private bool _passes;

        public string RelativePath
        {
            get;
        }

        public float Score
        {
            get;
        }

        public string BestTarget
        {
            get;
        }

        public string BestOutlineText
        {
            get;
        }

        public bool Passes
        {
            get => _passes;
            private set
            {
                _passes = value;
                OnPropertyChanged(nameof(Passes));
                OnPropertyChanged(nameof(VerdictDescription));
            }
        }

        public string VerdictDescription => _passes
            ? FreeAIr.Resources.Resources.RAG_calibration__passes
            : FreeAIr.Resources.Resources.RAG_calibration__cut_off
            ;

        public RagProbeResultViewModel(
            RagCandidate candidate,
            float minScore
            )
        {
            if (candidate is null)
            {
                throw new ArgumentNullException(nameof(candidate));
            }

            RelativePath = candidate.RelativePath;
            Score = candidate.Score;
            BestTarget = candidate.BestTarget;
            BestOutlineText = candidate.BestOutlineText;

            _passes = candidate.Score >= minScore;
        }

        public void ApplyThreshold(
            float minScore
            )
        {
            Passes = Score >= minScore;
        }
    }
}
