using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using LibVLCSharp.Shared;

namespace Sabeltann.Controls;

public class VideoView : NativeControlHost
{
    private IntPtr _childHwnd;
    private MediaPlayer? _mediaPlayer;

    public void Attach(MediaPlayer player)
    {
        _mediaPlayer = player;
        if (_childHwnd != IntPtr.Zero && OperatingSystem.IsWindows())
            player.Hwnd = _childHwnd;
    }

    public void Detach()
    {
        if (_mediaPlayer is not null)
            _mediaPlayer.Hwnd = IntPtr.Zero;
        _mediaPlayer = null;
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        if (OperatingSystem.IsWindows())
        {
            _childHwnd = Win32.CreateChildWindow(parent.Handle);
            if (_mediaPlayer is not null)
                _mediaPlayer.Hwnd = _childHwnd;
            return new Win32Handle(_childHwnd);
        }
        return base.CreateNativeControlCore(parent);
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        if (OperatingSystem.IsWindows() && _childHwnd != IntPtr.Zero)
        {
            if (_mediaPlayer is not null)
                _mediaPlayer.Hwnd = IntPtr.Zero;
            Win32.DestroyWindow(_childHwnd);
            _childHwnd = IntPtr.Zero;
        }
        base.DestroyNativeControlCore(control);
    }
}

file static class Win32
{
    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern IntPtr CreateWindowEx(
        int dwExStyle, string lpClassName, string lpWindowName,
        int dwStyle, int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool DestroyWindow(IntPtr hWnd);

    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    private static extern IntPtr CreateSolidBrush(int color);

    private const int COLORREF = 0x0011111b;
    private static readonly string ClassName = "SabeltannVid";
    private static readonly IntPtr HINSTANCE = System.Runtime.InteropServices.Marshal.GetHINSTANCE(typeof(Win32).Module);
    private static bool _registered;
    private static readonly Win32WndProc DelegateInstance = DefProc;

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private struct WNDCLASSEX
    {
        public uint cbSize;
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string? lpszMenuName;
        public string? lpszClassName;
        public IntPtr hIconSm;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private static void EnsureRegistered()
    {
        if (_registered) return;
        _registered = true;
        var brush = CreateSolidBrush(COLORREF);
        var wcx = new WNDCLASSEX
        {
            cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<WNDCLASSEX>(),
            lpfnWndProc = System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(DelegateInstance),
            hbrBackground = brush,
            hInstance = HINSTANCE,
            lpszClassName = ClassName,
        };
        RegisterClassEx(ref wcx);
    }

    private static IntPtr DefProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    public static IntPtr CreateChildWindow(IntPtr parentHwnd)
    {
        EnsureRegistered();
        return CreateWindowEx(0, ClassName, "",
            0x40000000 | 0x10000000 | 0x02000000 | 0x04000000,
            0, 0, 0, 0, parentHwnd, IntPtr.Zero, HINSTANCE, IntPtr.Zero);
    }

    private delegate IntPtr Win32WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
}

file class Win32Handle : IPlatformHandle
{
    public IntPtr Handle { get; }
    public string HandleDescriptor => "HWND";
    public Win32Handle(IntPtr handle) => Handle = handle;
}
