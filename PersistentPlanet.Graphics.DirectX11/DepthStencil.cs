using System;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace PersistentPlanet.Graphics.DirectX11
{
    public class DepthStencil : IDisposable
    {
        public DepthStencilView View { get; private set; }

        private DepthStencilState _depthStencilState;

        public void Initialise(D11InitialiseContext context)
        {
            var zBufferTextureDescription = new Texture2DDescription
            {
                Format = Format.D24_UNorm_S8_UInt,
                ArraySize = 1,
                MipLevels = 1,
                Width = context.RenderWindow.WindowWidth,
                Height = context.RenderWindow.WindowHeight,
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

            using (var zBufferTexture = new Texture2D(context.Device, zBufferTextureDescription))
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
                View = new DepthStencilView(context.Device, zBufferTexture, depthStencilViewDescription);
            }

            _depthStencilState = new DepthStencilState(context.Device, depthStencilDesc);
        }

        public void Dispose()
        {
            _depthStencilState?.Dispose();
            View?.Dispose();
        }

        public void Apply(D11RenderContext context)
        {
            context.Context.OutputMerger.SetDepthStencilState(_depthStencilState);
            context.Context.ClearDepthStencilView(View, DepthStencilClearFlags.Depth, 1f, 0);
        }
    }
}
