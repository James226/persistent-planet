using System;
using System.Diagnostics;
using MemBus;
using MemBus.Configurators;
using PersistentPlanet.Controls;
using PersistentPlanet.Primitives;
using SharpDX;
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
        private GameObject _cube;
        private Camera _camera;
        private Input _input;
        private IBus _bus;
        private bool _running;
        private DepthStencil _depthStencil;
        private GameObject _terrain;
        private Material _fullscreenMaterial;
        private RenderTexture _renderTexture;

        public Game(IRenderWindow renderWindow, IBus bus)
        {
            _renderWindow = renderWindow;
            _bus = bus;
        }

        public void Initialise()
        {
            _running = true;
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

            _renderTexture = new RenderTexture(_renderWindow);
            _renderTexture.Initialise(initialiseContext);


            _depthStencil = new DepthStencil();
            _depthStencil.Initialise(initialiseContext);

            _input = new Input(_bus);
            _input.Initialise(initialiseContext);

            _cube = new GameObject();
            _cube.AddComponent<Cube>();
            _cube.GetComponent<Transform>().Position = new Vector3(110, 7, 30);
            _cube.Initialise(initialiseContext);

            _terrain = new GameObject();
            _terrain.AddComponent<Terrain.Terrain>();
            _terrain.Initialise(initialiseContext);

            _camera = new Camera();
            _camera.Initialise(initialiseContext);

            _fullscreenMaterial = new Material((file, func) => new PixelShader(file, func, _renderTexture.Texture), (file, func) => new BasicVertexShader(file, func));
            _fullscreenMaterial.PixelShaderFilename = "fullscreen-quad.hlsl";
            _fullscreenMaterial.PixelShaderFunction = "PS";
            _fullscreenMaterial.VertexShaderFilename = "fullscreen-quad.hlsl";
            _fullscreenMaterial.VertexShaderFunction = "VS";
            _fullscreenMaterial.Initialise(initialiseContext);
        }

        public void Dispose()
        {
            _terrain?.Dispose();
            _cube?.Dispose();

            _depthStencil?.Dispose();
            _renderTexture?.Dispose();
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

            Win32.QueryPerformanceFrequency(out var counterFrequency);
            float ticksPerSecond = counterFrequency;

            Win32.QueryPerformanceCounter(out var lastTimestamp);
            var lastFps = lastTimestamp;

            while (_running && _renderWindow.NextFrame())
            {
                Win32.QueryPerformanceCounter(out var timestamp);

                var renderContext = new RenderContext { Context = _deviceContext, Bus = _bus, Input = _input, DeltaTime = (timestamp - lastTimestamp) / ticksPerSecond };

                _input.Update(renderContext);

                _deviceContext.OutputMerger.SetRenderTargets(_depthStencil.View, _renderTexture.View);
                _deviceContext.ClearRenderTargetView(_renderTexture.View, new RawColor4(.2f, .5f, .5f, 1f));
                //_deviceContext.OutputMerger.SetRenderTargets(_depthStencil.View, _renderTargetView);
                //_deviceContext.ClearRenderTargetView(_renderTargetView, new RawColor4(.2f, .5f, .5f, 1f));
                _depthStencil.Apply(renderContext);
                _deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

                _camera.Apply(renderContext);

                _cube.Render(renderContext);
                _terrain.Render(renderContext);

                _deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
                _deviceContext.OutputMerger.SetRenderTargets(_renderTargetView);
                _deviceContext.ClearRenderTargetView(_renderTargetView, new RawColor4(0, 0, 0, 1f));

                _fullscreenMaterial.Render(renderContext);
                _deviceContext.Draw(4, 0);

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