using System;
using System.Numerics;
using PersistentPlanet.Controls.Controls;
using PersistentPlanet.Graphics.Vulkan;
using SharpDX;
using VulkanCore;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace PersistentPlanet
{
    public class VulkanCamera : IDisposable
    {
        private Matrix4x4 _projectionMatrix;
        private Vector3 _cameraPosition;

        private Vector3 _velocity = Vector3.Zero;
        private Vector3 _rotation;
        private IDisposable _xAxisSubscription;
        private IDisposable _yAxisSubscription;
        private Vector2 _lastXAxis;
        private WorldBuffer _viewProjection;
        private VulkanBuffer _uniformBuffer;

        public void Initialise(VulkanInitialiseContext context)
        {
            //_projectionMatrix = Matrix4x4.PerspectiveFovLH((float)Math.PI / 3f, context.RenderWindow.WindowWidth / (float)context.RenderWindow.WindowHeight, .5f, 1000f);
            _cameraPosition = new Vector3(150f, 10f, 0);

            //_viewProjectionBuffer = new Buffer(context.Device,
            //                                   Utilities.SizeOf<Matrix>(),
            //                                   ResourceUsage.Default,
            //                                   BindFlags.ConstantBuffer,
            //                                   CpuAccessFlags.None,
            //                                   ResourceOptionFlags.None,
            //                                   0);

            _viewProjection = new WorldBuffer
            {
                Projection = Matrix4x4.CreatePerspectiveFieldOfView(
                    (float)Math.PI / 4,
                    (float)context.RenderWindow.WindowWidth / context.RenderWindow.WindowHeight,
                    1.0f,
                    1000.0f)
            };

            _uniformBuffer = context.WorldBuffer;

            _xAxisSubscription = context.Bus.Subscribe<XAxisUpdatedEvent>(OnXAxisUpdated);
            _yAxisSubscription = context.Bus.Subscribe<YAxisUpdatedEvent>(OnYAxisUpdated);
        }

        public void Dispose()
        {
            //_viewProjectionBuffer?.Dispose();
            _yAxisSubscription?.Dispose();
            _xAxisSubscription?.Dispose();
        }

        public void Apply(VulkanRenderContext context)
        {
            const float movementSpeed = 30;
            _cameraPosition += _velocity * context.DeltaTime * movementSpeed;

            var transform = Matrix4x4.CreateFromYawPitchRoll(_rotation.Y, _rotation.X, _rotation.Z);
            var lookAt = _cameraPosition + Vector3.Transform(Vector3.UnitZ, transform);
            var up = Vector3.Transform(-Vector3.UnitY, transform);
            //var viewMatrix = Matrix.LookAtLH(_cameraPosition, lookAt, up);
            //var viewProjection = Matrix.Multiply(viewMatrix, _projectionMatrix);
            //viewProjection.Transpose();

            //context.Context.UpdateSubresource(ref viewProjection, _viewProjectionBuffer);
            //context.Context.VertexShader.SetConstantBuffer(0, _viewProjectionBuffer);

            _viewProjection.View = Matrix4x4.CreateLookAt(_cameraPosition, lookAt, up);
            
            var ptr = _uniformBuffer.Memory.Map(0, Interop.SizeOf<WorldBuffer>());
            Interop.Write(ptr, ref _viewProjection);
            _uniformBuffer.Memory.Unmap();
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
            var transform = Matrix4x4.CreateFromYawPitchRoll(_rotation.Y, _rotation.X, _rotation.Z);
            _velocity = Vector3.Transform(new Vector3(_lastXAxis.X, 0, _lastXAxis.Y), transform);
        }
    }
}