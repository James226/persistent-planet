using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;

namespace PersistentPlanet.Graphics
{
    public class PixelShader : IShader
    {
        private readonly string _filename;
        private readonly string _function;
        private readonly Texture2D _texture2D;

        private SharpDX.Direct3D11.PixelShader _pixelShader;
        private ShaderResourceView _texture;

        public PixelShader(string filename, string function, Texture2D texture2d)
        {
            _filename = filename;
            _function = function;
            _texture2D = texture2d;
        }

        public void Initialise(IInitialiseContext context)
        {
            using (var byteCode = ShaderBytecode.CompileFromFile(_filename, _function, "ps_4_0", ShaderFlags.Debug))
            {
                _pixelShader = new SharpDX.Direct3D11.PixelShader(context.Device, byteCode);
            }

            _texture = new ShaderResourceView(context.Device, _texture2D);
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