using System;
using System.Runtime.InteropServices;
using MemBus;
using PersistentPlanet.Controls;
using PersistentPlanet.Graphics;
using PersistentPlanet.Window;

namespace PersistentPlanet
{
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
                new KeyboardManager(_bus),
                new MouseManager(_bus))
            {
                CursorVisible = false
            };
            _window.RelativeMouseMode = true;
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