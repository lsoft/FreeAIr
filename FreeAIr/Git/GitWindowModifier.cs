using EnvDTE;
using EnvDTE80;
using FreeAIr.Git;
using FreeAIr.UI;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.PlatformUI;
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
                                ToolTip = FreeAIr.Resources.Resources.FreeAIr_support__build_natural_language
                            };
                            buildNLOJsonFileButton.Click += BuildNLOJsonFileButton_Click;
                            vsPanel.Children.Insert(0, buildNLOJsonFileButton);
                            BuildNLOJsonFileButton = buildNLOJsonFileButton;

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
                //todo log
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
                //todo log
            }
        }

        private void BuildCommitMessageButton_Click(object sender, RoutedEventArgs e)
        {
            CommitMessageBuilder.ChooseAgentAsync(
                ).FileAndForget(nameof(CommitMessageBuilder));
        }
    }

}
