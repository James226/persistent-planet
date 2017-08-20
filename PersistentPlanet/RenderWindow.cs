using System;
using System.Runtime.InteropServices;

namespace PersistentPlanet
{
    public interface IRenderWindow
    {
        int WindowWidth { get; }
        int WindowHeight { get; }
        IntPtr Handle { get; }
        bool NextFrame();
    }

    public class RenderWindow : IRenderWindow
    {
        private readonly string _appName;
        private readonly string _className;
        private Win32.WNDCLASSEX _windowClass;
        public int WindowWidth { get; }
        public int WindowHeight { get; }

        public IntPtr Handle { get; private set; }

        public RenderWindow(string appName, string className, int windowWidth, int windowHeight)
        {
            _appName = appName;
            _className = className;
            WindowWidth = windowWidth;
            WindowHeight = windowHeight;
        }

        public bool Create()
        {
            return RegisterClass() && CreateWindow();
        }

        public bool NextFrame()
        {
            if (Win32.GetMessage(out Win32.MSG msg, Handle, 0, 0) < 0) return false;

            Win32.TranslateMessage(ref msg);
            Win32.DispatchMessage(ref msg);
            return msg.message != 130;
        }

        private bool RegisterClass()
        {
            _windowClass = new Win32.WNDCLASSEX
            {
                style = Win32.ClassStyles.DoubleClicks,
                lpfnWndProc = WndProc,
                cbClsExtra = 0,
                cbWndExtra = 0,
                hIcon = Win32.LoadIcon(IntPtr.Zero, (IntPtr)Win32.IDI_APPLICATION),
                hCursor = Win32.LoadCursor(IntPtr.Zero, (int)Win32.IDC_ARROW),
                hIconSm = IntPtr.Zero,
                hbrBackground = (IntPtr)(Win32.COLOR_WINDOW + 1),
                lpszMenuName = null,
                lpszClassName = _className
            };
            _windowClass.cbSize = (uint)Marshal.SizeOf(_windowClass);

            if (Win32.RegisterClassEx(ref _windowClass) == 0)
            {
                Win32.MessageBox(IntPtr.Zero, "RegisterClassEx failed", _appName,
                    (int)(Win32.MB_OK | Win32.MB_ICONEXCLAMATION | Win32.MB_SETFOREGROUND));
                return false;
            }
            return true;
        }

        private bool CreateWindow()
        {
            Handle = Win32.CreateWindowEx(0, _className, _appName, Win32.WS_OVERLAPPED | Win32.WS_CAPTION | Win32.WS_SYSMENU | Win32.WS_MINIMIZEBOX | Win32.WS_VISIBLE,
                250, 250, WindowWidth, WindowHeight, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            if (Handle != IntPtr.Zero)
                return true;
            Win32.MessageBox(IntPtr.Zero, "CreateWindow failed", _appName,
                (int)(Win32.MB_OK | Win32.MB_ICONEXCLAMATION | Win32.MB_SETFOREGROUND));
            return false;
        }

        private IntPtr WndProc(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam)
        {
            switch (message)
            {
                case Win32.WM_PAINT:
                    return IntPtr.Zero;
                case Win32.WM_DESTROY:
                    Win32.PostQuitMessage(0);
                    return IntPtr.Zero;
                default:
                    return Win32.DefWindowProc(hWnd, message, wParam, lParam);
            }
        }
    }
}