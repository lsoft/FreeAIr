using EnvDTE;
using EnvDTE80;
using FreeAIr.Git;
using FreeAIr.Helper;
using FreeAIr.Interaction;
using FreeAIr.UI;
using Microsoft.VisualStudio.Imaging;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xaml;
using WpfHelpers;

namespace FreeAIr.BLogic
{
    /// <summary>
    /// Injects the "Build commit message", "Add natural language outlines" and "Build NLO json file"
    /// buttons into Visual Studio's Git Changes pane, next to its own "Describe Changes" button. The
    /// pane is not FreeAIr's own window and gets rebuilt whenever it is docked, undocked or reopened,
    /// so this class keeps rescanning and re-inserting its buttons for as long as Visual Studio runs.
    /// </summary>
    [Export(typeof(GitWindowModifier))]
    [Export(typeof(IGitCommitMessageBox))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public sealed class GitWindowModifier : IGitCommitMessageBox
    {
        /// <summary>
        /// Put into <see cref="FrameworkElement.Tag"/> of the buttons this class inserts, so that a
        /// repeated scan recognizes its own work and does not add a second set of them.
        /// </summary>
        private const string CommitMessageButtonTag = "FreeAIr.BuildCommitMessage";
        /// <summary>
        /// The <see cref="FrameworkElement.Tag"/> marking the injected "Add natural language
        /// outlines" button.
        /// </summary>
        private const string OutlinesButtonTag = "FreeAIr.AddNaturalLanguageOutlines";
        /// <summary>
        /// The <see cref="FrameworkElement.Tag"/> marking the injected "Build NLO json file" button.
        /// </summary>
        private const string NLOJsonFileButtonTag = "FreeAIr.BuildNLOJsonFile";

        /// <summary>
        /// Used to detect the IDE shutting down, so the scan loop stops instead of outliving Visual
        /// Studio.
        /// </summary>
        private readonly DTEEvents _dteEvents;

        /// <summary>
        /// Cancelled on IDE shutdown, stopping the scan loop in <see cref="RunAsync"/>.
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// The Git Changes pane's own commit message text box, located by name; commit message
        /// generation writes its result here.
        /// </summary>
        public TextBox? CommitMessageTextBox
        {
            get;
            private set;
        }

        /// <summary>
        /// The injected button that generates a commit message with AI.
        /// </summary>
        public Button? BuildCommitMessageButton
        {
            get;
            private set;
        }

        /// <summary>
        /// The injected button that adds natural language outlines to the files of the pending diff.
        /// </summary>
        public Button? AddNaturalLanguageOutlinesButton
        {
            get;
            private set;
        }

        /// <summary>
        /// The injected button that opens the tool window for building the natural language
        /// outlines json file.
        /// </summary>
        public Button? BuildNLOJsonFileButton
        {
            get;
            private set;
        }

        /// <summary>
        /// Whether the Git Changes pane has been found and patched, meaning the commit message box
        /// and buttons are usable.
        /// </summary>
        public bool IsEnabled =>
            CommitMessageTextBox is not null
            && BuildCommitMessageButton is not null
            ;

        /// <inheritdoc/>
        bool IGitCommitMessageBox.IsAvailable => CommitMessageTextBox is not null;

        /// <inheritdoc/>
        void IGitCommitMessageBox.SetText(string text)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var textBox = CommitMessageTextBox;
            if (textBox is null)
            {
                return;
            }

            textBox.Text = text;
        }

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

        /// <summary>
        /// Polls the open windows for the Git Changes pane and, once found, inserts the commit
        /// message, outlines and NLO json file buttons next to its "Describe Changes" button. Keeps
        /// polling on a delay until found or cancelled, and rearms itself through
        /// <see cref="WatchForPanelTeardown"/> so the pane is patched again after it is rebuilt.
        /// </summary>
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

                            var cTextBox = w.GetRecursiveByName<TextBox>("textBox")
                                ?? w.GetRecursiveByName<TextBox>("commentTextBox");
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

                            var buildCommitMessageButton = CreateButtonFrom(dcButton);
                            buildCommitMessageButton.Content = new PseudoCrispImage
                            {
                                Moniker = KnownMonikers.GitRepository
                            };
                            buildCommitMessageButton.Tag = CommitMessageButtonTag;
                            buildCommitMessageButton.ToolTip = FreeAIr.Resources.Resources.FreeAIr_support__generate_commit;

                            buildCommitMessageButton.Click += BuildCommitMessageButton_Click;
                            vsPanel.Children.Insert(0, buildCommitMessageButton);
                            BuildCommitMessageButton = buildCommitMessageButton;



                            var addNaturalLanguageOutlinesButton = CreateButtonFrom(dcButton);
                            addNaturalLanguageOutlinesButton.Content = new PseudoCrispImage
                            {
                                Moniker = KnownMonikers.DocumentOutline
                            };
                            addNaturalLanguageOutlinesButton.Tag = OutlinesButtonTag;
                            addNaturalLanguageOutlinesButton.ToolTip = FreeAIr.Resources.Resources.FreeAIr_support__generate_and_add;

                            addNaturalLanguageOutlinesButton.Click += AddNaturalLanguageOutlinesButton_Click;
                            vsPanel.Children.Insert(0, addNaturalLanguageOutlinesButton);
                            AddNaturalLanguageOutlinesButton = addNaturalLanguageOutlinesButton;



                            var buildNLOJsonFileButton = CreateButtonFrom(dcButton);
                            buildNLOJsonFileButton.Content = new PseudoCrispImage
                            {
                                Moniker = KnownMonikers.ValidationSummary
                            };
                            buildNLOJsonFileButton.Tag = NLOJsonFileButtonTag;
                            buildNLOJsonFileButton.ToolTip = FreeAIr.Resources.Resources.FreeAIr_support__build_natural_language;

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

        /// <summary>
        /// Создает кнопку, похожу на входящую кнопку Visual Studio.
        /// </summary>
        private static Button CreateButtonFrom(Button vsButton)
        {
            var vsButtonType = vsButton.GetType();

            var result = (Button)Activator.CreateInstance(vsButtonType);

            // Копируем скрытое Attached/Dependency свойство "Kind"
            // Ищем статическое поле KindProperty в типе кнопки VS
            FieldInfo kindPropertyField = vsButtonType.GetField("KindProperty",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            if (kindPropertyField != null)
            {
                // Получаем сам объект DependencyProperty
                if (kindPropertyField.GetValue(null) is DependencyProperty kindProperty)
                {
                    // Читаем значение (например, "Subtle") из оригинала
                    object originalKindValue = vsButton.GetValue(kindProperty);

                    // Записываем его в нашу новую динамически созданную кнопку
                    result.SetValue(kindProperty, originalKindValue);
                }
            }

            result.HorizontalAlignment = HorizontalAlignment.Center;
            result.HorizontalContentAlignment = HorizontalAlignment.Center;
            result.Margin = new System.Windows.Thickness(0);
            result.Style = vsButton.Style;

            return result;
        }

        /// <summary>
        /// Looks for a button this class has already inserted into the given panel, identified by
        /// its <see cref="FrameworkElement.Tag"/>, so a repeated scan of the same pane does not
        /// insert a duplicate.
        /// </summary>
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

        /// <summary>
        /// Fires when the injected button is unloaded along with the Git Changes pane it lives in;
        /// clears the cached button/text box references and restarts <see cref="RunAsync"/> so the
        /// pane is found and patched again whether it was hidden or fully rebuilt.
        /// </summary>
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

        /// <summary>
        /// Opens the tool window for building the natural language outlines json file.
        /// </summary>
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

        /// <summary>
        /// Starts <see cref="GitNaturalLanguageOutliner.CollectOutlinesAsync"/> for the files of the
        /// pending git diff.
        /// </summary>
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

        /// <summary>
        /// Starts <see cref="CommitMessageBuilder.ChooseAgentAsync"/> to generate a commit message
        /// with AI.
        /// </summary>
        private void BuildCommitMessageButton_Click(object sender, RoutedEventArgs e)
        {
            CommitMessageBuilder.ChooseAgentAsync(
                ).FileAndForget(nameof(CommitMessageBuilder));
        }
    }

}
