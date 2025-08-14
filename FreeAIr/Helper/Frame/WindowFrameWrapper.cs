using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.Helper.Frame
{
    public class WindowFrameWrapper : IVsWindowFrameNotify, IVsWindowFrameNotify3, IDisposable
    {
        private IVsWindowFrame _windowFrame;
        private uint _cookie;
        private bool _isDisposed = false;

        public event Action<IVsWindowFrame> WindowClosed;
        public event Action<IVsWindowFrame> WindowSaved;

        public WindowFrameWrapper(IVsWindowFrame frame)
        {
            _windowFrame = frame ?? throw new ArgumentNullException(nameof(frame));
            Advise();
        }

        private void Advise()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_windowFrame is IVsWindowFrame2 frame2)
            {
                ErrorHandler.ThrowOnFailure(frame2.Advise(this, out _cookie));
            }
        }

        public int OnClose(ref uint pgrfSaveOptions)
        {
            // Вызывается перед закрытием
            System.Diagnostics.Debug.WriteLine("Window closing");
            return VSConstants.S_OK;
        }

        public int OnShow(int fShow)
        {
            // Отслеживаем закрытие окна
            if (fShow == (int)__FRAMESHOW.FRAMESHOW_WinClosed)
            {
                System.Diagnostics.Debug.WriteLine("Window closed");
                WindowClosed?.Invoke(_windowFrame);
                Cleanup();
            }

            return VSConstants.S_OK;
        }

        private void Cleanup()
        {
            if (!_isDisposed)
            {
                Unadvise();
            }
        }

        private void Unadvise()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_windowFrame is IVsWindowFrame2 frame2 && _cookie != 0)
            {
                frame2.Unadvise(_cookie);
                _cookie = 0;
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                Unadvise();
                _isDisposed = true;
            }
        }

        // Остальные методы интерфейса
        public int OnDockableChange(int fDockable, int x, int y, int w, int h) => VSConstants.S_OK;
        public int OnMove(int x, int y, int w, int h) => VSConstants.S_OK;
        public int OnSize(int x, int y, int w, int h) => VSConstants.S_OK;
        public int OnMove() => VSConstants.S_OK;
        public int OnSize() => VSConstants.S_OK;
        public int OnDockableChange(int fDockable) => VSConstants.S_OK;
    }
}
