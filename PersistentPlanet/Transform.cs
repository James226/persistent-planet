using SharpDX;

namespace PersistentPlanet
{
    public class Transform : IComponent
    {
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
                if (_isDirty)
                {
                    _transform = Matrix.Translation(_position);
                    _transform.Transpose();
                }
                return _transform;
            }
        }

        private Matrix _transform;
        private bool _isDirty = true;

        public void Initialise(InitialiseContext context)
        {
        }

        public void Dispose()
        {
            
        }

        public void Render(IRenderContext context)
        {
        }
    }
}