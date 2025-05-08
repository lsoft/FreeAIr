using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FreeAIr.Shared;
using FreeAIr.Shared.Dto;
using Microsoft;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.Language.CodeLens.Remoting;
using Microsoft.VisualStudio.Threading;

namespace FreeAIr.CodeLens
{

    public class CodeLensDataPoint : IAsyncCodeLensDataPoint, IDisposable
    {
        public static readonly CodeLensDetailEntryCommand AddXmlCommentCommand = new CodeLensDetailEntryCommand
        {
            CommandId = 0x1036, //must match with the command id from vsct file
            CommandSet = new Guid("faec8da8-74ca-4afa-8b7d-64be3914fbac") //must match with the command group guid from vsct file
        };
        public static readonly CodeLensDetailEntryCommand GenerateUnitTestsCommand = new CodeLensDetailEntryCommand
        {
            CommandId = 0x1037, //must match with the command id from vsct file
            CommandSet = new Guid("faec8da8-74ca-4afa-8b7d-64be3914fbac") //must match with the command group guid from vsct file
        };



        private readonly ICodeLensCallbackService _callbackService;
        private readonly CodeLensDescriptor _descriptor;

        private RemoteCodeLensConnectionHandler? _visualStudioConnection;
        private readonly ManualResetEventSlim _dataHasLoaded = new ManualResetEventSlim(initialState: false);

        private CodeLensUnitInfo? _codeLensUnitInfo;

        public event AsyncEventHandler? InvalidatedAsync;

        public CodeLensDescriptor Descriptor => this._descriptor;

        public Guid UniqueIdentifier
        {
            get;
        } = Guid.NewGuid();

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
        public void Refresh()
        {
            Invalidate();
        }

        public void Dispose()
        {
            _visualStudioConnection?.Dispose();
            _dataHasLoaded.Dispose();
        }

        #endregion

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
            catch (Exception ex)
            {
                //todo log
                throw;
            }
        }


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
                        //new List<CodeLensDetailPaneCommand>()
                        //{
                        //     new CodeLensDetailPaneCommand
                        //     {
                        //         CommandDisplayName = "Add XML comment",
                        //         CommandId = AddXmlCommentCommand,
                        //         CommandArgs = new object[] { _codeLensUnitInfo! }
                        //     },
                        //     new CodeLensDetailPaneCommand
                        //     {
                        //         CommandDisplayName = "Generate unit tests",
                        //         CommandId = GenerateUnitTestsCommand,
                        //         CommandArgs = new object[] { _codeLensUnitInfo! }
                        //     },
                        //},
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

        private static ImageId GetExtensionIcon()
        {
            return new ImageId(
                new Guid("{4edfe9dd-fe57-4a79-9c41-10d9eb76f4ae}"),
                1
                );
        }


        private static IEnumerable<CodeLensDetailEntryDescriptor> CreateEntries()
        {
            yield break;
        }

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
