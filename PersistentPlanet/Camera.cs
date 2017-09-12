using System;
using PersistentPlanet.Controls.Controls;
using PersistentPlanet.Graphics;
using PersistentPlanet.Graphics.DirectX11;
using SharpDX;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;
using Vector2 = System.Numerics.Vector2;

namespace PersistentPlanet
{
    public class Camera : IDisposable
    {
        private Matrix _projectionMatrix;
        private Vector3 _cameraPosition;
        private Buffer _viewProjectionBuffer;

        private Vector3 _velocity = Vector3.Zero;
        private Vector3 _rotation;
        private IDisposable _xAxisSubscription;
        private IDisposable _yAxisSubscription;
        private Vector2 _lastXAxis;

        public void Initialise(D11InitialiseContext context)
        {
            _projectionMatrix = Matrix.PerspectiveFovLH((float)Math.PI / 3f, context.RenderWindow.WindowWidth / (float)context.RenderWindow.WindowHeight, .5f, 1000f);
            _cameraPosition = new Vector3(150f, 10f, 0);

            _viewProjectionBuffer = new Buffer(context.Device,
                                               Utilities.SizeOf<Matrix>(),
                                               ResourceUsage.Default,
                                               BindFlags.ConstantBuffer,
                                               CpuAccessFlags.None,
                                               ResourceOptionFlags.None,
                                               0);

            _xAxisSubscription = context.Bus.Subscribe<XAxisUpdatedEvent>(OnXAxisUpdated);
            _yAxisSubscription = context.Bus.Subscribe<YAxisUpdatedEvent>(OnYAxisUpdated);
        }

        public void Dispose()
        {
            _viewProjectionBuffer?.Dispose();
            _yAxisSubscription?.Dispose();
            _xAxisSubscription?.Dispose();
        }

        public void Apply(D11RenderContext context)
        {
            const float movementSpeed = 30;
            _cameraPosition += _velocity * context.DeltaTime * movementSpeed;
            
            var transform = Matrix3x3.RotationYawPitchRoll(_rotation.Y, _rotation.X, _rotation.Z);
            var lookAt = _cameraPosition + Vector3.Transform(Vector3.ForwardLH, transform);
            var up = Vector3.Transform(Vector3.Up, transform);
            var viewMatrix = Matrix.LookAtLH(_cameraPosition, lookAt, up);
            var viewProjection = Matrix.Multiply(viewMatrix, _projectionMatrix);
            viewProjection.Transpose();

            context.Context.UpdateSubresource(ref viewProjection, _viewProjectionBuffer);
            context.Context.VertexShader.SetConstantBuffer(0, _viewProjectionBuffer);
        }

        private void OnYAxisUpdated(YAxisUpdatedEvent value)
        {
            _rotation += new Vector3(value.Axis.Y, value.Axis.X, 0) * 0.01f;
            UpdateVelocity();
        }

        private void OnXAxisUpdated(XAxisUpdatedEvent value)
        {
            _lastXAxis = value.Axis;
            UpdateVelocity();
        }

        private void UpdateVelocity()
        {
            var transform = Matrix3x3.RotationYawPitchRoll(_rotation.Y, _rotation.X, _rotation.Z);
            _velocity = Vector3.Transform(new Vector3(_lastXAxis.X, 0, _lastXAxis.Y), transform);
        }
    }
}