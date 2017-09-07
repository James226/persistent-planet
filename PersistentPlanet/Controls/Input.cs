using System;
using MemBus;
using PersistentPlanet.Controls.Controls;
using PersistentPlanet.Graphics;
using SharpDX;
using SharpDX.DirectInput;
using Vector2 = System.Numerics.Vector2;
using System.Linq;

namespace PersistentPlanet.Controls
{
    public class Input : IDisposable
    {
        private readonly IBus _bus;

        private DirectInput _input;
        private Keyboard _keyboard;
        private Mouse _mouse;

        private MouseState _lastMouseState;
        private bool _acquired;
        private bool _tryAcquire;

        public Input(IBus bus)
        {
            _bus = bus;
        }

        public void Initialise(InitialiseContext context)
        {
            _bus.Subscribe<WindowFocusChangedEvent>(e => _tryAcquire = e.HasFocus);
            _input = new DirectInput();

            _keyboard = new Keyboard(_input);
            _keyboard.SetCooperativeLevel(context.RenderWindow.Handle, CooperativeLevel.Foreground | CooperativeLevel.Exclusive);
            _keyboard.Acquire();

            _mouse = new Mouse(_input);
            _mouse.SetCooperativeLevel(context.RenderWindow.Handle, CooperativeLevel.Foreground | CooperativeLevel.Exclusive);
            _mouse.Acquire();

            _acquired = true;
        }

        public void Dispose()
        {
            _mouse?.Unacquire();
            _mouse?.Dispose();
            _keyboard?.Unacquire();
            _keyboard?.Dispose();
            _input?.Dispose();
        }

        public void Update(RenderContext context)
        {
            if (!_tryAcquire) return;
            if (!_acquired)
            {
                try
                {
                    _keyboard.Acquire();
                    _mouse.Acquire();
                }
                catch
                {
                }
            }

            try
            {

                var mouseState = _mouse.GetCurrentState();
                _lastMouseState = _lastMouseState ?? mouseState;
                var yAxis = new Vector2(mouseState.X, mouseState.Y);

                _lastMouseState = mouseState;
                context.Bus.Publish(new YAxisUpdatedEvent {Axis = yAxis});

                var keyboardState = _keyboard.GetCurrentState();
                if (keyboardState.PressedKeys.Any(k => k == Key.Escape))
                {
                    context.Bus.Publish(new EscapePressedEvent());
                }

                var xAxis = new Vector2();
                foreach (var key in keyboardState.PressedKeys)
                {
                    switch (key)
                    {
                        case Key.W:
                            xAxis.Y = 1;
                            break;
                        case Key.S:
                            xAxis.Y = -1;
                            break;
                        case Key.A:
                            xAxis.X = -1;
                            break;
                        case Key.D:
                            xAxis.X = 1;
                            break;
                    }
                }
                context.Bus.Publish(new XAxisUpdatedEvent { Axis = xAxis });

                var zAxis = new Vector2();
                foreach (var key in keyboardState.PressedKeys)
                {
                    switch (key)
                    {
                        case Key.Up:
                            zAxis.Y = 1;
                            break;
                        case Key.Down:
                            zAxis.Y = -1;
                            break;
                        case Key.Left:
                            zAxis.X = -1;
                            break;
                        case Key.Right:
                            zAxis.X = 1;
                            break;
                    }
                }
                context.Bus.Publish(new ZAxisUpdatedEvent { Axis = zAxis });
            }
            catch (SharpDXException e)
            {
                if (e.HResult == -2147024866)
                {
                    _acquired = false;
                }
            }

        }
    }
}
