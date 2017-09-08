using System;
using MemBus;
using PersistentPlanet.Graphics;
using PersistentPlanet.Graphics.DirectX11;
using SharpDX;

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
                _isDirty = true;
                ObjectBus.Publish(new PositionChangedEvent { Position = _position });
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
                _isDirty = true;
            }
        }
        private Quaternion _rotation;

        public Matrix Transformation
        {
            get
            {
                RecalculateTransform();
                return _transform;
            }
        }

        private Matrix _transform = Matrix.Identity;

        private bool _isDirty = true;
        private IDisposable _requestPositionUpdatedSubscription;

        public void Initialise(D11InitialiseContext context)
        {
            _requestPositionUpdatedSubscription = ObjectBus.Subscribe<RequestPositionUpdateEvent>(e => Position = e.Position);
        }

        public void Dispose()
        {
            _requestPositionUpdatedSubscription?.Dispose();
        }

        public void Render(D11RenderContext context)
        {
            RecalculateTransform();
        }

        private void RecalculateTransform()
        {
            if (!_isDirty) return;
            _transform = Matrix.Transformation(Vector3.Zero,
                                               Quaternion.Identity,
                                               Vector3.One,
                                               Vector3.Zero,
                                               _rotation,
                                               _position);
            _transform.Transpose();
            ObjectBus.Publish(new WorldMatrixUpdatedEvent {WorldMatrix = _transform});
        }
    }
}