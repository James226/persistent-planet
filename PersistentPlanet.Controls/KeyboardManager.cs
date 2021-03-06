﻿using MemBus;
using PersistentPlanet.Controls.Controls;
using PersistentPlanet.Primitives.Platform;

namespace PersistentPlanet.Controls
{
    public class KeyboardManager
    {
        private readonly IPublisher _publisher;
        private readonly IKeyboardAxisManager _xAxisManager;
        private readonly IKeyboardAxisManager _zAxisManager;

        public KeyboardManager(IPublisher publisher)
            : this(new KeyboardAxisManager<XAxisUpdatedEvent>(publisher, Key.W, Key.S, Key.A, Key.D),
                  new KeyboardAxisManager<ZAxisUpdatedEvent>(publisher, Key.Up, Key.Down, Key.Left, Key.Right))
        {
            _publisher = publisher;
        }

        public KeyboardManager(IKeyboardAxisManager xAxisManager, IKeyboardAxisManager zAxisManager)
        {
            _xAxisManager = xAxisManager;
            _zAxisManager = zAxisManager;
        }

        public void KeyUp(Key key)
        {
            _xAxisManager.KeyUp(key);
            _zAxisManager.KeyUp(key);
        }

        public void KeyDown(Key key)
        {
            if (key == Key.Escape)
            {
                _publisher.Publish(new EscapePressedEvent());
            }

            _xAxisManager.KeyDown(key);
            _zAxisManager.KeyDown(key);
        }
    }
}
