using System;
using MemBus;
using SharpDX;

namespace PersistentPlanet
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

        public void Initialise(InitialiseContext context)
        {
        }

        public void Dispose()
        {
        }

        public void Render(IRenderContext context)
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