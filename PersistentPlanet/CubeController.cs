using System;
using MemBus;
using PersistentPlanet.Controls;
using PersistentPlanet.Graphics;
using PersistentPlanet.Transform;
using SharpDX;

namespace PersistentPlanet
{
    public class CubeController : IComponent
    {
        public IBus ObjectBus { get; set; }

        private Vector3 _position;
        private IDisposable _positionChangedSubscription;
        private IDisposable _zAxisUpdatedSubscription;

        public void Initialise(InitialiseContext context)
        {
            _positionChangedSubscription = ObjectBus.Subscribe<PositionChangedEvent>(e => _position = e.Position);
            _zAxisUpdatedSubscription = context.Bus.Subscribe<ZAxisUpdatedEvent>(e =>
            {
                ObjectBus.Publish(new RequestPositionUpdateEvent
                {
                    Position = _position + new Vector3(e.ZAxis.X, 0, e.ZAxis.Y)
                });
            });
        }

        public void Dispose()
        {
            _positionChangedSubscription?.Dispose();
            _zAxisUpdatedSubscription?.Dispose();
        }

        public void Render(IRenderContext context)
        {
        }
    }
}
