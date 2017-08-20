using System;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace PersistentPlanet
{
    public class Game : IDisposable
    {
        private readonly IRenderWindow _renderWindow;
        private SwapChain _swapChain;

        private Texture2D _backBuffer;
        private RenderTargetView _renderTargetView;
        private Device _device;
        private DeviceContext _deviceContext;
        private GameObject _gameObject;
        private Buffer _worldViewProjectionBuffer;

        public Game(IRenderWindow renderWindow)
        {
            _renderWindow = renderWindow;
        }

        public void Initialise()
        {
            var backBufferDesc = new ModeDescription(_renderWindow.WindowWidth,
                                                     _renderWindow.WindowHeight,
                                                     new Rational(60, 1),
                                                     Format.R8G8B8A8_UNorm);

            var swapChainDesc = new SwapChainDescription
            {
                ModeDescription = backBufferDesc,
                SampleDescription = new SampleDescription(1, 0),
                Usage = Usage.RenderTargetOutput,
                BufferCount = 1,
                OutputHandle = _renderWindow.Handle,
                IsWindowed = true
            };

            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, swapChainDesc, out _device, out _swapChain);
            _deviceContext = _device.ImmediateContext;

            _backBuffer = _swapChain.GetBackBuffer<Texture2D>(0);
            _renderTargetView = new RenderTargetView(_device, _backBuffer);

            _deviceContext.Rasterizer.SetViewport(new RawViewportF
            {
                Height = _renderWindow.WindowHeight,
                Width = _renderWindow.WindowWidth
            });

            _gameObject = new GameObject();
            _gameObject.Initialise(_device, _deviceContext);

            _worldViewProjectionBuffer = new Buffer(_device,
                                                    Utilities.SizeOf<Matrix>(),
                                                    ResourceUsage.Default,
                                                    BindFlags.ConstantBuffer,
                                                    CpuAccessFlags.None,
                                                    ResourceOptionFlags.None,
                                                    0);

            var cameraPosition = new Vector3(0, 20f, 0);
            var cameraTarget = new Vector3(100, 0, 100);
            var cameraUp = Vector3.UnitY;
            var worldMatrix = Matrix.Identity;
            var viewMatrix = Matrix.LookAtLH(cameraPosition, cameraTarget, cameraUp);
            var projectionMatrix = Matrix.PerspectiveFovLH((float)Math.PI / 3f, _renderWindow.WindowWidth / (float)_renderWindow.WindowHeight, .5f, 1000f);
            var viewProjection = Matrix.Multiply(viewMatrix, projectionMatrix);
            var worldViewProjection = worldMatrix * viewProjection;
            worldViewProjection.Transpose();

            _deviceContext.UpdateSubresource(ref worldViewProjection, _worldViewProjectionBuffer);
            _deviceContext.VertexShader.SetConstantBuffer(0, _worldViewProjectionBuffer);
        }

        public void Dispose()
        {
            _gameObject.Dispose();

            _worldViewProjectionBuffer.Dispose();
            _renderTargetView.Dispose();
            _backBuffer.Dispose();
            _swapChain.Dispose();
            _device.Dispose();
            _deviceContext.Dispose();
        }

        public void Run()
        {
            var sw = new Stopwatch();
            sw.Start();
            var frame = 0;
            var lastFps = 0L;
            while (_renderWindow.NextFrame())
            {
                _deviceContext.OutputMerger.SetRenderTargets(_renderTargetView);
                _deviceContext.ClearRenderTargetView(_renderTargetView, new RawColor4(.2f, .5f, .5f, 1f));

                _gameObject.Render(_deviceContext);

                _swapChain.Present(1, PresentFlags.None);

                frame++;

                if (sw.ElapsedMilliseconds - lastFps <= 1000) continue;
                Console.WriteLine(frame);
                lastFps = sw.ElapsedMilliseconds;
                frame = 0;
            }
        }
    }
}