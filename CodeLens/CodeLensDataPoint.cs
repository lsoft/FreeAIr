using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FreeAIr.Shared;
using FreeAIr.Shared.Dto;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.Language.CodeLens.Remoting;
using Microsoft.VisualStudio.Threading;

namespace FreeAIr.CodeLens
{

    /// <summary>
    /// One CodeLens indicator instance for a single method. Runs in the CodeLens data-point process,
    /// separate from the main Visual Studio process, and talks back to it over the named pipe set up
    /// by <see cref="RemoteCodeLensConnectionHandler"/> to fetch <see cref="Shared.Dto.UnitInfo"/>.
    /// </summary>
    public class CodeLensDataPoint : IAsyncCodeLensDataPoint, IDisposable
    {
        /// <summary>Details-pane command that opens the "add XML comment" support action; the id and guid must stay in sync with the VSCT command definition.</summary>
        public static readonly CodeLensDetailEntryCommand AddXmlCommentCommand = new CodeLensDetailEntryCommand
        {
            CommandId = 0x1036, //must match with the command id from vsct file
            CommandSet = new Guid("faec8da8-74ca-4afa-8b7d-64be3914fbac") //must match with the command group guid from vsct file
        };
        /// <summary>Details-pane command that opens the "generate unit tests" support action; the id and guid must stay in sync with the VSCT command definition.</summary>
        public static readonly CodeLensDetailEntryCommand GenerateUnitTestsCommand = new CodeLensDetailEntryCommand
        {
            CommandId = 0x1037, //must match with the command id from vsct file
            CommandSet = new Guid("faec8da8-74ca-4afa-8b7d-64be3914fbac") //must match with the command group guid from vsct file
        };



        /// <summary>Callback channel used to invoke methods on the owning Visual Studio process, such as <see cref="ICodeLensListener.GetUnitInformationAsync"/>.</summary>
        private readonly ICodeLensCallbackService _callbackService;
        /// <summary>Identifies which code element (project, file, method) this data point represents.</summary>
        private readonly CodeLensDescriptor _descriptor;

        /// <summary>The named-pipe connection back to Visual Studio, opened once via <see cref="ConnectToVisualStudioAsync"/>.</summary>
        private RemoteCodeLensConnectionHandler? _visualStudioConnection;
        /// <summary>Signaled once <see cref="_codeLensUnitInfo"/> has been fetched, so a concurrent <see cref="GetDetailsAsync"/> call knows whether to wait or re-fetch.</summary>
        private readonly ManualResetEventSlim _dataHasLoaded = new ManualResetEventSlim(initialState: false);

        /// <summary>The most recently fetched unit info for this code element, cached between <see cref="GetDataAsync"/> and <see cref="GetDetailsAsync"/>.</summary>
        private CodeLensUnitInfo? _codeLensUnitInfo;

        /// <summary>Raised to tell Visual Studio this data point's indicator needs to be re-queried.</summary>
        public event AsyncEventHandler? InvalidatedAsync;

        /// <summary>The code element (project, file, method) this data point represents.</summary>
        public CodeLensDescriptor Descriptor => this._descriptor;

        /// <summary>Identifier this data point registers itself under with the Visual Studio side, so a targeted refresh can find it again.</summary>
        public Guid UniqueIdentifier
        {
            get;
        } = Guid.NewGuid();

        /// <summary>Creates the data point; the pipe to Visual Studio is opened separately via <see cref="ConnectToVisualStudioAsync"/>.</summary>
        public CodeLensDataPoint(
            ICodeLensCallbackService callbackService,
            CodeLensDescriptor descriptor
            )
        {
            if (callbackService is null)
            {
                throw new ArgumentNullException(nameof(callbackService));
            }

            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            _callbackService = callbackService;
            _descriptor = descriptor;
        }

        #region network related code

        /// <summary>Opens the named pipe to the owning Visual Studio process, identified by its PID.</summary>
        internal async Task ConnectToVisualStudioAsync(
            int vspid
            )
        {
            _visualStudioConnection = await RemoteCodeLensConnectionHandler
                .CreateAsync(owner: this, vspid)
                .ConfigureAwait(false)
                ;
        }

        // Called from VS via JSON RPC.
        /// <summary>Called remotely by Visual Studio to tell this data point its underlying data changed.</summary>
        public void Refresh()
        {
            Invalidate();
        }

        /// <summary>Closes the connection to Visual Studio and releases the wait handle.</summary>
        public void Dispose()
        {
            _visualStudioConnection?.Dispose();
            _dataHasLoaded.Dispose();
        }

        #endregion

        /// <summary>Builds the short text VS shows inline above the method (the CodeLens indicator's collapsed state).</summary>
        public async Task<CodeLensDataPointDescriptor> GetDataAsync(CodeLensDescriptorContext context, CancellationToken token)
        {
            try
            {
                _codeLensUnitInfo = await GetUnitInfoAsync(context, token);
                _dataHasLoaded.Set();

                var response123 = new CodeLensDataPointDescriptor()
                {
                    Description = $"FreeAIr support",
                    TooltipText = $"Available FreeAIr support commands",
                    IntValue = null, // no int value
                    ImageId = GetExtensionIcon(),
                };

                return response123;
            }
            catch (Exception excp)
            {
                //todo log
                throw;
            }
        }


        /// <summary>Builds the expanded details pane content shown when the user clicks the indicator.</summary>
        public async Task<CodeLensDetailsDescriptor> GetDetailsAsync(CodeLensDescriptorContext context, CancellationToken token)
        {
            try
            {
                // When opening the details pane, the data point is re-created leaving `data` uninitialized. VS will
                // then call `GetDataAsync()` and `GetDetailsAsync()` concurrently.
                if (!_dataHasLoaded.Wait(timeout: TimeSpan.FromSeconds(.5), token))
                {
                    _codeLensUnitInfo = await GetUnitInfoAsync(context, token);
                }

                var result = new CodeLensDetailsDescriptor()
                {
                    Headers = CreateHeaders(),
                    Entries = CreateEntries(),
                    CustomData =
                        _codeLensUnitInfo != null && _codeLensUnitInfo.UnitInfo != null
                            ? new List<object>() { _codeLensUnitInfo }
                            : new List<object>(),
                    PaneNavigationCommands = 
                        null,
                };

                return result;
            }
            catch (Exception ex)
            {
                //todo log
                throw;
            }
        }

        /// <summary>
        /// Raises <see cref="IAsyncCodeLensDataPoint.Invalidated"/> event.
        /// </summary>
        /// <remarks>
        ///  This is not part of the IAsyncCodeLensDataPoint interface.
        ///  The data point source can call this method to notify the client proxy that data for this data point has changed.
        /// </remarks>
        public void Invalidate()
        {
            _dataHasLoaded.Reset();
            this.InvalidatedAsync?.Invoke(this, EventArgs.Empty).ConfigureAwait(false);
        }


        /// <summary>Builds a <see cref="CodeLensTarget"/> from the descriptor/context and calls back into Visual Studio via <see cref="ICodeLensListener.GetUnitInformationAsync"/> to resolve it.</summary>
        private async Task<CodeLensUnitInfo?> GetUnitInfoAsync(
            CodeLensDescriptorContext context,
            CancellationToken token
            )
        {
            CodeLensUnitInfo? result = null;

            try
            {
                var d = new Dictionary<string, string>();
                foreach (var pair in context.Properties)
                {
                    d[pair.Key.ToString()] = pair.Value.ToString();
                }

                var methodName = _descriptor.ElementDescription;
                var liofd = methodName.LastIndexOf(".");
                if (liofd > 0 && liofd < (methodName.Length - 1))
                {
                    methodName = methodName.Substring(liofd + 1);
                }
                d["MethodName"] = methodName;

                result = await _callbackService
                    .InvokeAsync<CodeLensUnitInfo>(
                        this,
                        nameof(ICodeLensListener.GetUnitInformationAsync),
                        new object[]
                        {
                            new CodeLensTarget(
                                _descriptor.ProjectGuid,
                                _descriptor.FilePath,
                                d,
                                context.ApplicableSpan.HasValue ? context.ApplicableSpan.Value.Start : (int?)null,
                                context.ApplicableSpan.HasValue ? context.ApplicableSpan.Value.Length : (int?)null
                                )
                        },
                        token
                        )
                    .ConfigureAwait(false)
                    ;
            }
            catch (Exception ex)
            {
                //todo log
            }

            return result;
        }

        /// <summary>Icon id shown next to the CodeLens indicator text.</summary>
        private static ImageId GetExtensionIcon()
        {
            return new ImageId(
                new Guid("{4edfe9dd-fe57-4a79-9c41-10d9eb76f4ae}"),
                1
                );
        }


        /// <summary>Row entries for the details pane; currently none — the pane is populated via <see cref="CodeLensDetailsDescriptor.CustomData"/> instead.</summary>
        private static IEnumerable<CodeLensDetailEntryDescriptor> CreateEntries()
        {
            yield break;
        }

        /// <summary>Column headers for the details pane; currently none, kept for a future tabular layout.</summary>
        private static List<CodeLensDetailHeaderDescriptor> CreateHeaders()
        {
            return new List<CodeLensDetailHeaderDescriptor>()
            {
                //new CodeLensDetailHeaderDescriptor
                //{
                //    DisplayName = "FreeAIr available support",
                //    IsVisible = true,
                //    UniqueName = "UniqueName",
                //    Width = 1.0
                //}
            };
        }

    }
}
