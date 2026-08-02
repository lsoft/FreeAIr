using EnvDTE;
using EnvDTE80;
using FreeAIr.Git;
using FreeAIr.Helper;
using FreeAIr.UI;
using Microsoft.VisualStudio.Imaging;
using System.ComponentModel.Composition;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfHelpers;

namespace FreeAIr.BLogic
{
    [Export(typeof(GitWindowModifier))]
    public sealed class GitWindowModifier
    {
        /// <summary>
        /// Put into <see cref="FrameworkElement.Tag"/> of the buttons this class inserts, so that a
        /// repeated scan recognizes its own work and does not add a second set of them.
        /// </summary>
        private const string CommitMessageButtonTag = "FreeAIr.BuildCommitMessage";
        private const string OutlinesButtonTag = "FreeAIr.AddNaturalLanguageOutlines";
        private const string NLOJsonFileButtonTag = "FreeAIr.BuildNLOJsonFile";

        private readonly DTEEvents _dteEvents;

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public TextBox? CommitMessageTextBox
        {
            get;
            private set;
        }

        public Button? BuildCommitMessageButton
        {
            get;
            private set;
        }

        public Button? AddNaturalLanguageOutlinesButton
        {
            get;
            private set;
        }

        public Button? BuildNLOJsonFileButton
        {
            get;
            private set;
        }

        public bool IsEnabled => 
            CommitMessageTextBox is not null
            && BuildCommitMessageButton is not null
            ;

        [ImportingConstructor]
        public GitWindowModifier()
        {
            var dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
            if (dte is null)
            {
                throw new InvalidOperationException("Cannot obtain DTE service.");
            }

            _dteEvents = ((Events2)dte.Events).DTEEvents;
            _dteEvents.OnBeginShutdown += DTEEvents_OnBeginShutdown;
        }

        private void DTEEvents_OnBeginShutdown()
        {
            _cancellationTokenSource.Cancel();
        }

        public async Task RunAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var cancellationToken = _cancellationTokenSource.Token;

                while (true)
                {
                    try
                    {
                        foreach (System.Windows.Window w in Application.Current.Windows)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                return;
                            }

                            var dcButton = w.GetRecursiveByName<Button>("describeChangesButton");
                            if (dcButton is null)
                            {
                                continue;
                            }

                            var vsButtonPanel = VisualTreeHelper.GetParent(dcButton);
                            if (vsButtonPanel is not StackPanel vsPanel)
                            {
                                continue;
                            }

                            var cTextBox = w.GetRecursiveByName<TextBox>("textBox");
                            if (cTextBox is null)
                            {
                                continue;
                            }

                            CommitMessageTextBox = cTextBox;

                            var alreadyInsertedButton = FindOwnButton(vsPanel, CommitMessageButtonTag);
                            if (alreadyInsertedButton is not null)
                            {
                                //панель уже дополнена: её всего лишь скрывали и показали снова.
                                //Второй комплект кнопок здесь не нужен, нужны лишь ссылки на них
                                BuildCommitMessageButton = alreadyInsertedButton;
                                AddNaturalLanguageOutlinesButton = FindOwnButton(vsPanel, OutlinesButtonTag);
                                BuildNLOJsonFileButton = FindOwnButton(vsPanel, NLOJsonFileButtonTag);

                                WatchForPanelTeardown(alreadyInsertedButton);
                                return;
                            }

                            var buildCommitMessageButton = new Button
                            {
                                Content = new PseudoCrispImage
                                {
                                    Moniker = KnownMonikers.GitRepository
                                },
                                HorizontalAlignment = HorizontalAlignment.Center,
                                HorizontalContentAlignment = HorizontalAlignment.Center,
                                Margin = new System.Windows.Thickness(0),
                                Style = dcButton.Style,
                                Tag = CommitMessageButtonTag,
                                ToolTip = FreeAIr.Resources.Resources.FreeAIr_support__generate_commit
                            };
                            buildCommitMessageButton.Click += BuildCommitMessageButton_Click;
                            vsPanel.Children.Insert(0, buildCommitMessageButton);
                            BuildCommitMessageButton = buildCommitMessageButton;

                            var addNaturalLanguageOutlinesButton = new Button
                            {
                                Content = new PseudoCrispImage
                                {
                                    Moniker = KnownMonikers.DocumentOutline
                                },
                                HorizontalAlignment = HorizontalAlignment.Center,
                                HorizontalContentAlignment = HorizontalAlignment.Center,
                                Margin = new System.Windows.Thickness(0),
                                Style = dcButton.Style,
                                Tag = OutlinesButtonTag,
                                ToolTip = FreeAIr.Resources.Resources.FreeAIr_support__generate_and_add
                            };
                            addNaturalLanguageOutlinesButton.Click += AddNaturalLanguageOutlinesButton_Click;
                            vsPanel.Children.Insert(0, addNaturalLanguageOutlinesButton);
                            AddNaturalLanguageOutlinesButton = addNaturalLanguageOutlinesButton;

                            var buildNLOJsonFileButton = new Button
                            {
                                Content = new PseudoCrispImage
                                {
                                    Moniker = KnownMonikers.ValidationSummary
                                },
                                HorizontalAlignment = HorizontalAlignment.Center,
                                HorizontalContentAlignment = HorizontalAlignment.Center,
                                Margin = new System.Windows.Thickness(0),
                                Style = dcButton.Style,
                                Tag = NLOJsonFileButtonTag,
                                ToolTip = FreeAIr.Resources.Resources.FreeAIr_support__build_natural_language
                            };
                            buildNLOJsonFileButton.Click += BuildNLOJsonFileButton_Click;
                            vsPanel.Children.Insert(0, buildNLOJsonFileButton);
                            BuildNLOJsonFileButton = buildNLOJsonFileButton;

                            WatchForPanelTeardown(buildCommitMessageButton);
                            return;
                        }

                        await Task.Delay(5_000, cancellationToken);
                    }
                    catch (Exception excp)
                    {
                        //todo

                        //not to ddos VS UI
                        await Task.Delay(15_000, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                //this is ok
            }
            catch (Exception excp)
            {
                //todo
            }
        }

        private static Button? FindOwnButton(
            StackPanel panel,
            string tag
            )
        {
            foreach (var child in panel.Children)
            {
                if (child is Button button && (button.Tag as string) == tag)
                {
                    return button;
                }
            }

            return null;
        }

        /// <summary>
        /// Visual Studio rebuilds the Git Changes pane whenever it is docked, undocked or reopened,
        /// and our buttons go away together with the panel they were inserted into. The scan stops
        /// as soon as the panel is found, so without this it would never notice the loss and the
        /// buttons would be gone for the rest of the session.
        /// </summary>
        private void WatchForPanelTeardown(
            Button ownButton
            )
        {
            ownButton.Unloaded -= OwnButtonUnloaded;
            ownButton.Unloaded += OwnButtonUnloaded;
        }

        private void OwnButtonUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                button.Unloaded -= OwnButtonUnloaded;
            }

            CommitMessageTextBox = null;
            BuildCommitMessageButton = null;
            AddNaturalLanguageOutlinesButton = null;
            BuildNLOJsonFileButton = null;

            if (_cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            //панель могли всего лишь скрыть, а могли и пересоздать; разберёмся в самом сканировании
            RunAsync()
                .FileAndForget(nameof(GitWindowModifier));
        }

        private void BuildNLOJsonFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BuildNaturalLanguageOutlinesJsonFileToolWindow.ShowPaneAsync(
                    false
                    ).FileAndForget(nameof(BuildNaturalLanguageOutlinesJsonFileToolWindow));
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }
        }

        private void AddNaturalLanguageOutlinesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                GitNaturalLanguageOutliner.CollectOutlinesAsync(
                    ).FileAndForget(nameof(GitNaturalLanguageOutliner));
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }
        }

        private void BuildCommitMessageButton_Click(object sender, RoutedEventArgs e)
        {
            CommitMessageBuilder.ChooseAgentAsync(
                ).FileAndForget(nameof(CommitMessageBuilder));
        }
    }

}
