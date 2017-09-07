using System;
using System.Numerics;
using MemBus;
using PersistentPlanet.Controls.Controls;
using PersistentPlanet.Primitives.Platform;

namespace PersistentPlanet.Controls
{
    public class KeyboardManager
    {
        private readonly IPublisher _publisher;
        private Vector2 _xAxis;

        public KeyboardManager(IPublisher publisher)
        {
            _publisher = publisher;
        }

        public void KeyUp(Key key)
        {
            var value = GetXAxisValue(key);
            if (value == Vector2.Zero) return;

            _xAxis -= value;
            _publisher.Publish(new XAxisUpdatedEvent {XAxis = _xAxis});
        }

        public void KeyDown(Key key)
        {
            var value = GetXAxisValue(key);
            if (value == Vector2.Zero) return;

            _xAxis += value;
            _publisher.Publish(new XAxisUpdatedEvent {XAxis = _xAxis });
        }

        private static Vector2 GetXAxisValue(Key key)
        {
            switch (key)
            {
                case Key.W:
                    return Vector2.UnitY;
                case Key.S:
                    return -Vector2.UnitY;
                case Key.A:
                    return -Vector2.UnitX;
                case Key.D:
                    return Vector2.UnitX;
                default: return Vector2.Zero;
            }
        }
    }
}
