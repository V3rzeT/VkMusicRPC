namespace VkMusicRpc;

public class WindowStateWatcher
{
    public delegate void Notify();

    private bool _lastState;

    private readonly Thread _watcherThread;
    private readonly IntPtr _windowHandle;

    public WindowStateWatcher(IntPtr windowHandle)
    {
        _watcherThread = new Thread(ThreadWork);
        _windowHandle = windowHandle;
        _watcherThread.Start();
    }

    public event Notify WindowMinimized;

    private void ThreadWork()
    {
        while (true)
        {
            // Window Minimized Check
            if (NativeMethods.IsWindowMinimized(_windowHandle))
            {
                if (_lastState != true)
                {
                    WindowMinimized?.Invoke();
                    _lastState = true;
                }
            }
            else
            {
                if (_lastState)
                    _lastState = false;
            }

            Thread.Sleep(1); // CPU usage fix
        }
    }
}