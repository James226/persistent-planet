using System;
using MemBus;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using Device = SharpDX.Direct3D11.Device;

namespace PersistentPlanet.Graphics.DirectX11
{
    public class D11Renderer : IGenericRenderer<D11InitialiseContext, D11RenderContext>, IDisposable
    {
        private Device _device;
        private DeviceContext _deviceContext;
        private SwapChain _swapChain;
        private Texture2D _backBuffer;
        private RenderTargetView _renderTargetView;
        private RenderTexture _renderTexture;
        private DepthStencil _depthStencil;
        private Material _fullscreenMaterial;

        public IGenericShader<D11InitialiseContext, D11RenderContext> CreateShader()
        {
            return null;
        }

        public (D11InitialiseContext, Func<D11RenderContext>) Initialise(IRenderWindow renderWindow, IBus bus)
        {
            var backBufferDesc = new ModeDescription(renderWindow.WindowWidth,
                renderWindow.WindowHeight,
                new Rational(60, 1),
                Format.R8G8B8A8_UNorm);

            var swapChainDesc = new SwapChainDescription
            {
                ModeDescription = backBufferDesc,
                SampleDescription = new SampleDescription(1, 0),
                Usage = Usage.RenderTargetOutput,
                BufferCount = 1,
                OutputHandle = renderWindow.Handle,
                IsWindowed = true
            };

            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, swapChainDesc, out _device, out _swapChain);
            _deviceContext = _device.ImmediateContext;

            _backBuffer = _swapChain.GetBackBuffer<Texture2D>(0);
            _renderTargetView = new RenderTargetView(_device, _backBuffer);

            _deviceContext.Rasterizer.SetViewport(new RawViewportF
            {
                Height = renderWindow.WindowHeight,
                Width = renderWindow.WindowWidth,
                MinDepth = 0,
                MaxDepth = 1
            });

            var initialiseContext = new D11InitialiseContext
            {
                Device = _device,
                RenderWindow = renderWindow,
                Bus = bus
            };

            _renderTexture = new RenderTexture(renderWindow.WindowWidth, renderWindow.WindowHeight);
            _renderTexture.Initialise(initialiseContext);

            _depthStencil = new DepthStencil();
            _depthStencil.Initialise(initialiseContext);

            _fullscreenMaterial = new Material((file, func) => new PixelShader(file, func, _renderTexture.Texture),
                (file, func) => new BasicVertexShader(file, func))
            {
                PixelShaderFilename = "fullscreen-quad.hlsl",
                PixelShaderFunction = "PS",
                VertexShaderFilename = "fullscreen-quad.hlsl",
                VertexShaderFunction = "VS"
            };
            _fullscreenMaterial.Initialise(initialiseContext);

            Win32.QueryPerformanceFrequency(out var counterFrequency);
            float ticksPerSecond = counterFrequency;

            Win32.QueryPerformanceCounter(out var lastTimestamp);

            var renderContext = new D11RenderContext { Context = _deviceContext, Bus = bus };

            D11RenderContext CreateRenderContext()
            {
                Win32.QueryPerformanceCounter(out var timestamp);
                renderContext.DeltaTime = (timestamp - lastTimestamp) / ticksPerSecond;
                lastTimestamp = timestamp;
                return renderContext;
            }

            return (initialiseContext, CreateRenderContext);
        }

        public void Dispose()
        {
            _renderTargetView?.Dispose();
            _backBuffer?.Dispose();
            _swapChain?.Dispose();
            _deviceContext?.Dispose();
            _device?.Dispose();
        }

        public void Render(D11RenderContext context, Action render)
        {
            _deviceContext.OutputMerger.SetRenderTargets(_depthStencil.View, _renderTexture.View);
            _deviceContext.ClearRenderTargetView(_renderTexture.View, new RawColor4(.2f, .5f, .5f, 1f));

            _depthStencil.Apply(context);
            _deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            render?.Invoke();

            _deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
            _deviceContext.OutputMerger.SetRenderTargets(_renderTargetView);
            _deviceContext.ClearRenderTargetView(_renderTargetView, new RawColor4(0, 0, 0, 1f));

            _fullscreenMaterial.Render(context);
            _deviceContext.Draw(4, 0);

            _swapChain.Present(1, PresentFlags.None);
        }
    }
}