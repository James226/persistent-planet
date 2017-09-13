using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;

namespace PersistentPlanet.Graphics.DirectX11
{
    public class TexturePixelShader : IShader
    {
        private readonly string _filename;
        private readonly string _function;
        private readonly Texture2D _texture2d;

        private SharpDX.Direct3D11.PixelShader _pixelShader;
        private ShaderResourceView _texture;
        private SamplerState _samplerState;

        public TexturePixelShader(string filename, string function, Texture2D texture = null)
        {
            _filename = filename;
            _function = function;
            _texture2d = texture;
        }

        public void Initialise(D11InitialiseContext context)
        {
            using (var byteCode = ShaderBytecode.CompileFromFile(_filename, _function, "ps_4_0", ShaderFlags.Debug))
            {
                _pixelShader = new SharpDX.Direct3D11.PixelShader(context.Device, byteCode);
            }

            _samplerState = new SamplerState(context.Device, new SamplerStateDescription
            {
                Filter = Filter.ComparisonMinMagMipLinear,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                MipLodBias = 0,
                MaximumAnisotropy = 1,
                ComparisonFunction = Comparison.Always,
                BorderColor = new RawColor4(0, 0, 0, 0),
                MinimumLod = 0,
                MaximumLod = float.MaxValue
            });

            if (_texture2d == null)
            {
                using (var bitmap = TextureLoader.LoadBitmap(new SharpDX.WIC.ImagingFactory2(), "sand.jpg"))
                using (var texture = TextureLoader.CreateTexture2DFromBitmap(context.Device, bitmap))
                {
                    _texture = new ShaderResourceView(context.Device, texture);
                }
            }
            else
            {
                _texture = new ShaderResourceView(context.Device, _texture2d);
            }
        }

        public void Dispose()
        {
            _samplerState?.Dispose();
            _pixelShader?.Dispose();
            _texture?.Dispose();
        }

        public void Apply(D11RenderContext renderContext)
        {
            renderContext.Context.PixelShader.Set(_pixelShader);
            renderContext.Context.PixelShader.SetShaderResource(0, _texture);
            renderContext.Context.PixelShader.SetSampler(0, _samplerState);
        }
    }
}