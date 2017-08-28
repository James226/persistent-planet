using System;
using System.Diagnostics;
using System.Linq;
using MemBus;
using MemBus.Configurators;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DirectInput;
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
        private DepthStencilView _depthStencilView;
        private Input _input;
        private IBus _bus;
        private bool _running;

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
                WindowSize = new Vector2(_renderWindow.WindowWidth, _renderWindow.WindowHeight),
                RenderWindow = _renderWindow,
                Bus = _bus
            };

            _input = new Input();
            _input.Initialise(initialiseContext);

            _gameObject = new GameObject();
            _gameObject.Initialise(initialiseContext);


            var zBufferTextureDescription = new Texture2DDescription
            {
                Format = Format.D24_UNorm_S8_UInt,
                ArraySize = 1,
                MipLevels = 1,
                Width = _renderWindow.WindowWidth,
                Height = _renderWindow.WindowHeight,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            var depthStencilDesc = new DepthStencilStateDescription
            {
                BackFace = new DepthStencilOperationDescription
                {
                    FailOperation = StencilOperation.Keep,
                    PassOperation = StencilOperation.Keep,
                    DepthFailOperation = StencilOperation.Decrement,
                    Comparison = Comparison.Always
                },
                FrontFace = new DepthStencilOperationDescription
                {
                    FailOperation = StencilOperation.Keep,
                    PassOperation = StencilOperation.Keep,
                    DepthFailOperation = StencilOperation.Increment,
                    Comparison = Comparison.Always
                },
                IsDepthEnabled = true,
                DepthWriteMask = DepthWriteMask.All,
                DepthComparison = Comparison.Less,

                IsStencilEnabled = true,
                StencilReadMask = 0xFF,
                StencilWriteMask = 0xFF

            };
            using (var zBufferTexture = new Texture2D(_device, zBufferTextureDescription))
            {
                var depthStencilViewDescription = new DepthStencilViewDescription
                {
                    Format = Format.D24_UNorm_S8_UInt,
                    Dimension = DepthStencilViewDimension.Texture2D,
                    Texture2D = new DepthStencilViewDescription.Texture2DResource
                    {
                        MipSlice = 0
                    }
                };
                _depthStencilView = new DepthStencilView(_device, zBufferTexture, depthStencilViewDescription);
            }

            var depthStencilState = new DepthStencilState(_device, depthStencilDesc);
            _deviceContext.OutputMerger.SetDepthStencilState(depthStencilState);

            _camera = new Camera();
            _camera.Initialise(initialiseContext);
        }

        public void Dispose()
        {
            _gameObject?.Dispose();

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
            var lastFps = 0L;
            while (_running && _renderWindow.NextFrame())
            {
                var renderContext = new RenderContext { Context = _deviceContext, Bus = _bus, Input = _input };

                _input.Update(renderContext);

                _deviceContext.OutputMerger.SetRenderTargets(_depthStencilView, _renderTargetView);
                _deviceContext.ClearRenderTargetView(_renderTargetView, new RawColor4(.2f, .5f, .5f, 1f));
                _deviceContext.ClearDepthStencilView(_depthStencilView, DepthStencilClearFlags.Depth, 1f, 0);

                _camera.Apply(renderContext);

                _gameObject.Render(renderContext);

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