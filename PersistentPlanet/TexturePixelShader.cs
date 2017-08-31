using MemBus;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;

namespace PersistentPlanet
{
    public class TexturePixelShader : IShader
    {
        private readonly string _filename;
        private readonly string _function;

        private SharpDX.Direct3D11.PixelShader _pixelShader;
        private ShaderResourceView _texture;
        private Texture2D _texture2d;

        public TexturePixelShader(string filename, string function, Texture2D texture = null)
        {
            _filename = filename;
            _function = function;
            _texture2d = texture;
        }

        public void Initialise(IInitialiseContext context)
        {
            using (var byteCode = ShaderBytecode.CompileFromFile(_filename, _function, "ps_4_0", ShaderFlags.Debug))
            {
                _pixelShader = new SharpDX.Direct3D11.PixelShader(context.Device, byteCode);
            }

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
            _pixelShader?.Dispose();
            _texture?.Dispose();
        }

        public void Apply(IRenderContext renderContext)
        {
            renderContext.Context.PixelShader.Set(_pixelShader);
            renderContext.Context.PixelShader.SetShaderResource(0, _texture);
        }
    }
}