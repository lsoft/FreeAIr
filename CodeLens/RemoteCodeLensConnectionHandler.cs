using System;
using System.IO.Pipes;
using System.Threading.Tasks;
using FreeAIr.Shared;
using StreamJsonRpc;

namespace FreeAIr.CodeLens
{
    /// <summary>
    /// Client side of the CodeLens named pipe, living in the out-of-process CodeLens host: connects one
    /// <see cref="CodeLensDataPoint"/> to the main Visual Studio process and relays refresh calls back to it.
    /// Taken from  https://github.com/bert2/microscope completely.
    /// Take a look to that repo, it's amazing!
    /// </summary>
    public class RemoteCodeLensConnectionHandler : IRemoteCodeLens, IDisposable
    {
        /// <summary>The CodeLens data point this handler represents on the Visual Studio side of the pipe.</summary>
        private readonly CodeLensDataPoint _owner;
        /// <summary>The named pipe client connected to the Visual Studio process hosting <see cref="_owner"/>.</summary>
        private readonly NamedPipeClientStream _stream;
        /// <summary>The JSON-RPC channel attached to <see cref="_stream"/> once the connection is open.</summary>
        private JsonRpc? _rpc;

        /// <summary>Creates a handler and connects its pipe to the Visual Studio side before returning it.</summary>
        public async static Task<RemoteCodeLensConnectionHandler> CreateAsync(CodeLensDataPoint owner, int vspid)
        {
            var handler = new RemoteCodeLensConnectionHandler(owner, vspid);
            await handler.ConnectAsync().ConfigureAwait(false);
            return handler;
        }

        /// <summary>Prepares (but does not open) the named pipe to the given Visual Studio process's <see cref="Shared.ICodeLensListener"/>.</summary>
        public RemoteCodeLensConnectionHandler(CodeLensDataPoint owner, int vspid)
        {
            _owner = owner;
            _stream = new NamedPipeClientStream(
                serverName: ".",
                CodeLensPipeName.Get(vspid),
                PipeDirection.InOut,
                PipeOptions.Asynchronous
                );
        }

        /// <summary>Closes the named pipe connection to Visual Studio.</summary>
        public void Dispose() => _stream.Dispose();

        /// <summary>Called remotely by the Visual Studio side to tell this data point its data changed.</summary>
        public void Refresh() => _owner.Refresh();

        /// <summary>Opens the named pipe and registers <see cref="_owner"/> with the Visual Studio side over JSON-RPC.</summary>
        private async Task ConnectAsync()
        {
            await _stream.ConnectAsync().ConfigureAwait(false);
            _rpc = JsonRpc.Attach(_stream, this);
            await _rpc.InvokeAsync(nameof(IRemoteVisualStudioCodeLens.RegisterCodeLensDataPoint), _owner.UniqueIdentifier).ConfigureAwait(false);
        }

    }
}
