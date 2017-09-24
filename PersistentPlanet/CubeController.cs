using System;
using System.Numerics;
using MemBus;
using PersistentPlanet.Controls.Controls;
using PersistentPlanet.Graphics;
using PersistentPlanet.Transform;

namespace PersistentPlanet
{
    public class CubeController : IComponent
    {
        public IBus ObjectBus { get; set; }

        private Vector3 _position;
        private IDisposable _positionChangedSubscription;
        private IDisposable _zAxisUpdatedSubscription;

        public void Initialise(IInitialiseContext context, IResourceCollection resourceCollection)
        {
            _positionChangedSubscription = ObjectBus.Subscribe<PositionChangedEvent>(e => _position = e.Position);
            _zAxisUpdatedSubscription = context.Bus.Subscribe<ZAxisUpdatedEvent>(e =>
            {
                ObjectBus.Publish(new RequestPositionUpdateEvent
                {
                    Position = _position + new Vector3(e.Axis.X, 0, e.Axis.Y)
                });
            });
        }

        public void Dispose()
        {
            _positionChangedSubscription?.Dispose();
            _zAxisUpdatedSubscription?.Dispose();
        }
    }
}
