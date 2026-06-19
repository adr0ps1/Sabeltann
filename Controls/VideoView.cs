using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Platform;
using LibVLCSharp.Shared;

namespace Sabeltann.Controls;

public class VideoView : NativeControlHost
{
    private IntPtr _childHwnd;
    private MediaPlayer? _mediaPlayer;

    public event Action? MouseActivity;

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
            _childHwnd = Native.SubclassCreate(parent.Handle, this);
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
            Native.SubclassRemove(_childHwnd);
            if (_mediaPlayer is not null)
                _mediaPlayer.Hwnd = IntPtr.Zero;
            Native.DestroyWindow(_childHwnd);
            _childHwnd = IntPtr.Zero;
        }
        base.DestroyNativeControlCore(control);
    }

    public void OnMouseActivity() => MouseActivity?.Invoke();
}

file static class Native
{
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowEx(
        int dwExStyle, string lpClassName, string lpWindowName,
        int dwStyle, int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    public static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateSolidBrush(int color);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowLongPtrW(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowLongPtrA(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private const int WM_MOUSEMOVE = 0x0200;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int GWLP_WNDPROC = -4;

    private const int COLORREF = 0x0011111b;
    private static readonly string ClassName = "SabeltannVid";
    private static readonly IntPtr HINSTANCE = Marshal.GetHINSTANCE(typeof(Native).Module);
    private static bool _registered;

    private static readonly WndProcDelegate ClassWndProc = StaticWndProc;
    private static readonly WndProcDelegate SubclassWndProc = SubClassProc;

    private static readonly Dictionary<IntPtr, (IntPtr OriginalWndProc, WeakReference<VideoView> View)> _hooks = new();

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
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

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private static IntPtr StaticWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        return DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    private static void EnsureRegistered()
    {
        if (_registered) return;
        _registered = true;
        var brush = CreateSolidBrush(COLORREF);
        var wcx = new WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(ClassWndProc),
            hbrBackground = brush,
            hInstance = HINSTANCE,
            lpszClassName = ClassName,
        };
        RegisterClassEx(ref wcx);
    }

    public static IntPtr SubclassCreate(IntPtr parentHwnd, VideoView view)
    {
        EnsureRegistered();
        var hwnd = CreateWindowEx(0, ClassName, "",
            0x40000000 | 0x10000000 | 0x02000000 | 0x04000000,
            0, 0, 0, 0, parentHwnd, IntPtr.Zero, HINSTANCE, IntPtr.Zero);

        var originalProc = SetWindowLongPtrW(hwnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(SubclassWndProc));
        _hooks[hwnd] = (originalProc, new WeakReference<VideoView>(view));
        return hwnd;
    }

    public static void SubclassRemove(IntPtr hwnd)
    {
        if (_hooks.TryGetValue(hwnd, out var hook))
        {
            SetWindowLongPtrW(hwnd, GWLP_WNDPROC, hook.OriginalWndProc);
            _hooks.Remove(hwnd);
        }
    }

    private static IntPtr SubClassProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_MOUSEMOVE || msg == WM_LBUTTONDOWN || msg == WM_LBUTTONUP)
        {
            if (_hooks.TryGetValue(hWnd, out var hook) && hook.View.TryGetTarget(out var view))
                Avalonia.Threading.Dispatcher.UIThread.Post(view.OnMouseActivity);
        }
        if (_hooks.TryGetValue(hWnd, out var entry))
            return CallWindowProc(entry.OriginalWndProc, hWnd, msg, wParam, lParam);
        return DefWindowProcW(hWnd, msg, wParam, lParam);
    }
}

file class Win32Handle : IPlatformHandle
{
    public IntPtr Handle { get; }
    public string HandleDescriptor => "HWND";
    public Win32Handle(IntPtr handle) => Handle = handle;
}