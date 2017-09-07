using System.Numerics;
using MemBus;
using PersistentPlanet.Controls.Controls;

namespace PersistentPlanet.Controls
{
    public class MouseManager
    {
        private readonly IPublisher _publisher;

        public MouseManager(IPublisher publisher)
        {
            _publisher = publisher;
        }

        public void Move(int x, int y)
        {
            _publisher.Publish(new YAxisUpdatedEvent { Axis = new Vector2(x, y)});
        }
    }
}
