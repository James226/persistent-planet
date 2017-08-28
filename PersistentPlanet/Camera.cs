using System;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DirectInput;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace PersistentPlanet
{
    public interface ICamera
    {
        Matrix ViewProjection { get; }
    }

    public class Camera : IObserver<XAxisUpdatedEvent>, IDisposable
    {
        private Matrix _projectionMatrix;
        private Vector3 _cameraPosition;
        private Buffer _viewProjectionBuffer;

        private Vector3 _rotation;
        private IDisposable _xAxisSubscription;

        public void Initialise(InitialiseContext context)
        {
            _projectionMatrix = Matrix.PerspectiveFovLH((float)Math.PI / 3f, context.WindowSize.X / context.WindowSize.Y, .5f, 1000f);
            _cameraPosition = new Vector3(150f, 10f, 0);

            _viewProjectionBuffer = new Buffer(context.Device,
                                               Utilities.SizeOf<Matrix>(),
                                               ResourceUsage.Default,
                                               BindFlags.ConstantBuffer,
                                               CpuAccessFlags.None,
                                               ResourceOptionFlags.None,
                                               0);

            _xAxisSubscription = context.Bus.Subscribe<XAxisUpdatedEvent>(OnNext);
            
        }

        public void Dispose()
        {
            _viewProjectionBuffer?.Dispose();
            _xAxisSubscription?.Dispose();
        }

        public void Apply(RenderContext context)
        {
            //if (context.KeyboardState.IsPressed(Key.W))
            //    _cameraPosition.Z = _cameraPosition.Z + 0.1f;

            var transform = Matrix3x3.RotationYawPitchRoll(_rotation.Y, _rotation.X, _rotation.Z);
            var lookAt = _cameraPosition + Vector3.Transform(Vector3.ForwardLH, transform);
            var up = Vector3.Transform(Vector3.Up, transform);
            var viewMatrix = Matrix.LookAtLH(_cameraPosition, lookAt, up);
            var viewProjection = Matrix.Multiply(viewMatrix, _projectionMatrix);
            viewProjection.Transpose();

            context.Context.UpdateSubresource(ref viewProjection, _viewProjectionBuffer);
            context.Context.VertexShader.SetConstantBuffer(0, _viewProjectionBuffer);
        }

        public void OnNext(XAxisUpdatedEvent value)
        {
            _rotation += new Vector3(value.XAxis.Y, value.XAxis.X, 0) * 0.01f;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }
    }
}