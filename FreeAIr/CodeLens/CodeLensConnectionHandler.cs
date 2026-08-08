using FreeAIr.Helper;
using FreeAIr.Shared;
using StreamJsonRpc;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;

namespace FreeAIr.Extension.CodeLens
{
    /// <summary>
    /// Server side of the CodeLens named pipe, living in the main Visual Studio process: accepts one
    /// connection per <see cref="CodeLensDataPoint"/> and tracks them in <see cref="_connections"/> so
    /// they can all be told to refresh via <see cref="RefreshAllCodeLensDataPointsAsync"/>.
    /// </summary>
    public class CodeLensConnectionHandler : IRemoteVisualStudioCodeLens, IDisposable
    {
        /// <summary>Every currently-connected CodeLens data point, keyed by the id it registered with, so a refresh can be dispatched to a specific one.</summary>
        private static readonly ConcurrentDictionary<Guid, CodeLensConnectionHandler> _connections = new ();

        /// <summary>The JSON-RPC channel attached to this handler's named pipe connection.</summary>
        private JsonRpc? _rpc;
        /// <summary>The id of the CodeLens data point this handler serves, set once it registers itself.</summary>
        private Guid? _dataPointId;

        /// <summary>Runs forever, accepting one named-pipe connection per CodeLens data point and handing each off to <see cref="CodeLensConnectionHandler"/>.</summary>
        public static async Task AcceptCodeLensConnectionsAsync()
        {
            try
            {
                while (true)
                {
                    var stream = new NamedPipeServerStream(
                        CodeLensPipeName.Get(Process.GetCurrentProcess().Id),
                        PipeDirection.InOut,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);
                    await stream.WaitForConnectionAsync().ConfigureAwait(false);
                    _ = HandleConnectionAsync(stream);
                }
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
                throw;
            }

            /// <summary>Attaches JSON-RPC to one accepted pipe connection and keeps it alive until the data point disconnects.</summary>
            static async Task HandleConnectionAsync(NamedPipeServerStream stream)
            {
                try
                {
                    using (var handler = new CodeLensConnectionHandler())
                    {
                        var rpc = JsonRpc.Attach(stream, handler);
                        handler._rpc = rpc;
                        await rpc.Completion;
                    }
                }
                catch (Exception excp)
                {
                    excp.ActivityLogException();
                }
                finally
                {
                    stream.Dispose();
                }
            }
        }

        /// <summary>Tells every currently-connected CodeLens data point to re-query its data, e.g. after a background analysis result changes.</summary>
        public static Task RefreshAllCodeLensDataPointsAsync()
        {
            try
            {
                return Task.WhenAll(_connections.Keys.Select(RefreshCodeLensDataPointAsync));
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }

            return Task.CompletedTask;
        }

        /// <summary>Unregisters this handler's data point from <see cref="_connections"/> so refreshes stop targeting a disconnected pipe.</summary>
        public void Dispose()
        {
            if (_dataPointId.HasValue)
            {
                _ = _connections.TryRemove(_dataPointId.Value, out _);
            }
        }

        // Called from each CodeLensDataPoint via JSON RPC.
        /// <summary>Registers a connecting data point under <paramref name="id"/> so it can later be targeted for a refresh.</summary>
        public void RegisterCodeLensDataPoint(Guid id)
        {
            _dataPointId = id;
            _connections[id] = this;
        }


        /// <summary>Invokes <see cref="IRemoteCodeLens.Refresh"/> on the data point registered under <paramref name="id"/>.</summary>
        private static Task RefreshCodeLensDataPointAsync(Guid id)
        {
            if (!_connections.TryGetValue(id, out var conn))
            {
                throw new InvalidOperationException($"CodeLens data point {id} was not registered.");
            }

            if (conn == null)
            {
                return Task.CompletedTask;
            }

            return conn._rpc!.InvokeAsync(nameof(IRemoteCodeLens.Refresh));
        }

    }

}
