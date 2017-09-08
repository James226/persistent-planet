using System.Numerics;
using MemBus;
using PersistentPlanet.Controls.Controls;
using PersistentPlanet.Primitives.Platform;

namespace PersistentPlanet.Controls
{
    public interface IKeyboardAxisManager
    {
        void KeyUp(Key key);
        void KeyDown(Key key);
    }

    public class KeyboardAxisManager<T> : IKeyboardAxisManager where T : IAxisUpdatedEvent, new()
    {
        private readonly IPublisher _publisher;
        private Vector2 _axis;
        private readonly Key _up;
        private readonly Key _down;
        private readonly Key _left;
        private readonly Key _right;

        public KeyboardAxisManager(IPublisher publisher, Key up, Key down, Key left, Key right)
        {
            _publisher = publisher;
            _up = up;
            _down = down;
            _left = left;
            _right = right;
        }

        public void KeyUp(Key key)
        {
            var value = GetAxisValue(key);
            if (value == Vector2.Zero) return;

            _axis -= value;
            _publisher.Publish(new T { Axis = _axis });
        }

        public void KeyDown(Key key)
        {
            var value = GetAxisValue(key);
            if (value == Vector2.Zero) return;

            _axis += value;
            _publisher.Publish(new T { Axis = _axis });
        }

        private Vector2 GetAxisValue(Key key)
        {
            if (key == _up)
                return Vector2.UnitY;
            if (key == _down)
                return -Vector2.UnitY;
            if (key == _left)
                return -Vector2.UnitX;
            if (key == _right)
                return Vector2.UnitX;
            return Vector2.Zero;
        }
    }
}
