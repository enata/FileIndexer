using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;

namespace FileIndexer.DemoApp
{
    internal sealed class OldWindow : IWin32Window
    {
        private readonly System.IntPtr _handle;

        public OldWindow(Window window)
        {
            var source = (System.Windows.Interop.HwndSource)PresentationSource.FromVisual(window);
            Debug.Assert(source != null, "source != null");
            _handle = source.Handle;
        }

        System.IntPtr IWin32Window.Handle
        {
            get { return _handle; }
        }

    }
}