using System;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace PersistentPlanet.Graphics.DirectX11
{
    public class RenderTexture : IDisposable
    {
        public RenderTargetView View => _renderTargetView;
        public Texture2D Texture => _texture;

        private Texture2D _texture;
        private RenderTargetView _renderTargetView;
        private ShaderResourceView _shaderResourceView;
        private int _width;
        private int _height;

        public RenderTexture(int width, int height)
        {
            _width = width;
            _height = height;
        }

        public void Initialise(InitialiseContext context)
        {
            int width = _width;
            int height = _height;

            var textureDescription = new Texture2DDescription
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.R32G32B32A32_Float,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            _texture = new Texture2D(context.Device, textureDescription);

            var renderTargetViewDescription = new RenderTargetViewDescription
            {
                Format = textureDescription.Format,
                Dimension = RenderTargetViewDimension.Texture2D,
                Texture2D = new RenderTargetViewDescription.Texture2DResource {MipSlice = 0}
            };

            _renderTargetView = new RenderTargetView(context.Device, _texture, renderTargetViewDescription);

            var shaderResourceViewDescription = new ShaderResourceViewDescription
            {
                Format = textureDescription.Format,
                Dimension = ShaderResourceViewDimension.Texture2D,
                Texture2D = new ShaderResourceViewDescription.Texture2DResource
                {
                    MostDetailedMip = 0,
                    MipLevels = 1
                }
            };

            _shaderResourceView = new ShaderResourceView(context.Device, _texture, shaderResourceViewDescription);
        }

        public void Dispose()
        {
            _shaderResourceView.Dispose();
            _renderTargetView.Dispose();
            _texture.Dispose();
        }

        public void Apply(RenderContext context)
        {
            context.Context.OutputMerger.SetRenderTargets((RenderTargetView) _renderTargetView);
        }
    }
}