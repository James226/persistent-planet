using System;
using System.Numerics;
using MemBus;
using PersistentPlanet.Graphics;

namespace PersistentPlanet.Transform
{
    public class Transform : IComponent
    {
        public IBus ObjectBus { get; set; }

        public Vector3 Position
        {
            get => _position;
            set
            {
                if (_position == value) return;
                _position = value;
                ObjectBus.Publish(new PositionChangedEvent { Position = _position });
                RecalculateTransform();
            }
        }
        private Vector3 _position;

        public Quaternion Rotation
        {
            get => _rotation;
            set
            {
                if (_rotation == value) return;
                _rotation = value;
                RecalculateTransform();
            }
        }
        private Quaternion _rotation;
        
        private IDisposable _requestPositionUpdatedSubscription;

        public void Initialise(IInitialiseContext context, IResourceCollection resourceCollection)
        {
            _requestPositionUpdatedSubscription = ObjectBus.Subscribe<RequestPositionUpdateEvent>(e => Position = e.Position);
        }

        public void Dispose()
        {
            _requestPositionUpdatedSubscription?.Dispose();
        }

        private void RecalculateTransform()
        {
            var transform = Matrix4x4.CreateTranslation(_position)
                            * Matrix4x4.CreateFromQuaternion(_rotation)
                            * Matrix4x4.CreateScale(1);

            ObjectBus.Publish(new WorldMatrixUpdatedEvent {WorldMatrix = transform});
        }
    }
}