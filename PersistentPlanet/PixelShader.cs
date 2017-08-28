using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;

namespace PersistentPlanet
{
    public class PixelShader : IShader
    {
        private readonly string _filename;
        private readonly string _function;

        private SharpDX.Direct3D11.PixelShader _pixelShader;
        private ShaderResourceView _texture;

        public PixelShader(string filename, string function)
        {
            _filename = filename;
            _function = function;
        }

        public void Initialise(IInitialiseContext context)
        {
            using (var byteCode = ShaderBytecode.CompileFromFile(_filename, _function, "ps_4_0", ShaderFlags.Debug))
            {
                _pixelShader = new SharpDX.Direct3D11.PixelShader(context.Device, byteCode);
            }

            using (var bitmap = TextureLoader.LoadBitmap(new SharpDX.WIC.ImagingFactory2(), "sand.jpg"))
            using (var texture = TextureLoader.CreateTexture2DFromBitmap(context.Device, bitmap))
            {
                _texture = new ShaderResourceView(context.Device, texture);
            }
        }

        public void Dispose()
        {
            _pixelShader?.Dispose();
            _texture?.Dispose();
        }

        public void Apply(IRenderContext renderContext, GameObject gameObject)
        {
            renderContext.Context.PixelShader.Set(_pixelShader);
            renderContext.Context.PixelShader.SetShaderResource(0, _texture);
        }
    }
}