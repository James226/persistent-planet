using System;
using System.Diagnostics;
using MemBus;
using MemBus.Configurators;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
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
        private Camera _camera;
        private Input _input;
        private IBus _bus;
        private bool _running;
        private DepthStencil _depthStencil;

        public Game(IRenderWindow renderWindow)
        {
            _renderWindow = renderWindow;
        }

        public void Initialise()
        {
            _running = true;
            _bus = BusSetup.StartWith<Conservative>().Construct();
            _bus.Subscribe<EscapePressedEvent>(_ => _running = false);

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
                Width = _renderWindow.WindowWidth,
                MinDepth = 0,
                MaxDepth = 1
            });

            var initialiseContext = new InitialiseContext
            {
                Device = _device,
                RenderWindow = _renderWindow,
                Bus = _bus
            };

            _depthStencil = new DepthStencil();
            _depthStencil.Initialise(initialiseContext);

            _input = new Input();
            _input.Initialise(initialiseContext);

            _gameObject = new GameObject();
            _gameObject.Initialise(initialiseContext);

            _camera = new Camera();
            _camera.Initialise(initialiseContext);
        }

        public void Dispose()
        {
            _gameObject?.Dispose();

            _depthStencil?.Dispose();
            _renderTargetView?.Dispose();
            _backBuffer?.Dispose();
            _swapChain?.Dispose();
            _input?.Dispose();
            _device?.Dispose();
            _deviceContext?.Dispose();
            _bus?.Dispose();
        }

        public void Run()
        {
            var sw = new Stopwatch();
            sw.Start();
            var frame = 0;

            Win32.QueryPerformanceFrequency(out long counterFrequency);
            float ticksPerSecond = counterFrequency;

            Win32.QueryPerformanceCounter(out long lastTimestamp);
            var lastFps = lastTimestamp;

            while (_running && _renderWindow.NextFrame())
            {
                Win32.QueryPerformanceCounter(out long timestamp);

                var renderContext = new RenderContext { Context = _deviceContext, Bus = _bus, Input = _input, DeltaTime = (timestamp - lastTimestamp) / ticksPerSecond };

                _input.Update(renderContext);

                _deviceContext.OutputMerger.SetRenderTargets(_depthStencil.View, _renderTargetView);
                _deviceContext.ClearRenderTargetView(_renderTargetView, new RawColor4(.2f, .5f, .5f, 1f));
                _depthStencil.Apply(renderContext);

                _camera.Apply(renderContext);

                _gameObject.Render(renderContext);

                _swapChain.Present(1, PresentFlags.None);

                frame++;

                lastTimestamp = timestamp;
                if (timestamp - lastFps <= counterFrequency) continue;
                Console.WriteLine(frame);
                lastFps = timestamp;
                frame = 0;
            }

            sw.Stop();
        }
    }
}