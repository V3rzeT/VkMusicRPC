using System.Runtime.InteropServices;

namespace VkMusicRpc;

public class NativeMethods
{
    public const int SW_HIDE = 0;
    public const int SW_SHOW = 5;
    public const int WS_MINIMIZE = 0x20000000;
    public const int GWL_STYLE = -16;

    [DllImport("kernel32.dll")]
    public static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    public static bool IsWindowMinimized(IntPtr windowHandle)
    {
        int style = GetWindowLong(windowHandle, GWL_STYLE);
        if ((style & WS_MINIMIZE) != 0)
            return true;

        return false;
    }
}