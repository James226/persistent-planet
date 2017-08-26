using System;
using SharpDX;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace PersistentPlanet
{
    public interface ICamera
    {
        Matrix ViewProjection { get; }
    }

    public class Camera
    {
        private Matrix _projectionMatrix;
        private Vector3 _cameraPosition;
        private Vector3 _cameraTarget;
        private Vector3 _cameraUp;
        private Buffer _viewProjectionBuffer;

        public void Initialise(InitialiseContext context)
        {
            _projectionMatrix = Matrix.PerspectiveFovLH((float)Math.PI / 3f, context.WindowSize.X / context.WindowSize.Y, .5f, 1000f);

            _cameraPosition = new Vector3(150f, 10f, 0);
            _cameraTarget = new Vector3(100, 0, 100);
            _cameraUp = Vector3.UnitY;

            _viewProjectionBuffer = new Buffer(context.Device,
                                               Utilities.SizeOf<Matrix>(),
                                               ResourceUsage.Default,
                                               BindFlags.ConstantBuffer,
                                               CpuAccessFlags.None,
                                               ResourceOptionFlags.None,
                                               0);
        }

        public void Apply(RenderContext context)
        {
            _cameraPosition.Z = _cameraPosition.Z + 0.1f;
            var viewMatrix = Matrix.LookAtLH(_cameraPosition, _cameraTarget, _cameraUp);
            var viewProjection = Matrix.Multiply(viewMatrix, (Matrix) _projectionMatrix);
            viewProjection.Transpose();

            context.Context.UpdateSubresource(ref viewProjection, _viewProjectionBuffer);
            context.Context.VertexShader.SetConstantBuffer(0, _viewProjectionBuffer);
        }
    }
}