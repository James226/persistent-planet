using System;
using System.Runtime.InteropServices;
using MemBus;
using PersistentPlanet.Controls;
using PersistentPlanet.Primitives.Platform;
using PersistentPlanet.Window;

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
        private readonly IBus _bus;
        private Win32.WNDCLASSEX _windowClass;
        private bool _hasFocus;
        public int WindowWidth { get; }
        public int WindowHeight { get; }

        public IntPtr Handle { get; private set; }

        public RenderWindow(string appName, string className, int windowWidth, int windowHeight, IBus bus)
        {
            _appName = appName;
            _className = className;
            _bus = bus;
            WindowWidth = windowWidth;
            WindowHeight = windowHeight;
        }

        public bool Create()
        {
            return RegisterClass() && CreateWindow();
        }

        public bool NextFrame()
        {
            var hasFocus = Win32.GetActiveWindow() == Handle;

            if (hasFocus != _hasFocus)
            {
                _bus.Publish(new WindowFocusChangedEvent {HasFocus = hasFocus});
                _hasFocus = hasFocus;
            }

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

    public class SdlWindow : IRenderWindow
    {
        private Sdl2Window _window;
        private string _appName;
        private IBus _bus;
        public int WindowWidth { get; }
        public int WindowHeight { get; }
        public IntPtr Handle => _window.Handle;

        public SdlWindow(string appName, string className, int windowWidth, int windowHeight, IBus bus)
        {
            _appName = appName;
            _bus = bus;
            WindowWidth = windowWidth;
            WindowHeight = windowHeight;
        }

        public void Create()
        {
            _window = new Sdl2Window(_appName,
                100,
                100,
                WindowWidth,
                WindowHeight,
                SDL_WindowFlags.Resizable | SDL_WindowFlags.OpenGL,
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                new KeyboardManager(_bus))
            {
                CursorVisible = false
            };
            //_window.WindowState = WindowState.BorderlessFullScreen;
        }

        public void Dispose()
        {
            _window.Close();
        }

        public bool NextFrame()
        {
            _window.PumpEvents();
            return _window.Exists;
        }
    }
}