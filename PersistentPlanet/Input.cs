using System;
using MemBus.Support;
using SharpDX;
using SharpDX.DirectInput;
using System.Linq;

namespace PersistentPlanet
{
    public interface IInput
    {
        Vector2 XAxis { get; }
    }

    public class Input : IInput, IDisposable
    {
        public Vector2 XAxis { get; private set; }

        private DirectInput _input;
        private Keyboard _keyboard;
        private Mouse _mouse;

        private MouseState _lastMouseState;

        public void Initialise(InitialiseContext context)
        {
            _input = new DirectInput();

            _keyboard = new Keyboard(_input);
            _keyboard.SetCooperativeLevel(context.RenderWindow.Handle, CooperativeLevel.Foreground | CooperativeLevel.Exclusive);
            _keyboard.Acquire();

            _mouse = new Mouse(_input);
            _mouse.SetCooperativeLevel(context.RenderWindow.Handle, CooperativeLevel.Foreground | CooperativeLevel.Exclusive);
            _mouse.Acquire();
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
            var mouseState = _mouse.GetCurrentState();
            _lastMouseState = _lastMouseState ?? mouseState;
            XAxis = new Vector2(mouseState.X, mouseState.Y);

            _lastMouseState = mouseState;
            context.Bus.Publish(new XAxisUpdatedEvent { XAxis = XAxis });

            var keyboardState = _keyboard.GetCurrentState();
            if (keyboardState.PressedKeys.Any(k => k == Key.Escape))
            {
                context.Bus.Publish(new EscapePressedEvent());
            }
        }
    }

    public class EscapePressedEvent
    {
    }

    public class XAxisUpdatedEvent
    {
        public Vector2 XAxis { get; set; }
    }
}
