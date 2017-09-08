using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;

namespace PersistentPlanet.Graphics.DirectX11
{
    public class BasicVertexShader : IShader
    {
        private readonly string _filename;
        private readonly string _function;
        private VertexShader _vertexShader;

        public BasicVertexShader(string filename, string function)
        {
            _filename = filename;
            _function = function;
        }

        public void Initialise(D11InitialiseContext context)
        {
            using (var vertexShaderByteCode =
                ShaderBytecode.CompileFromFile(_filename, _function, "vs_4_0", ShaderFlags.Debug))
            {
                _vertexShader = new VertexShader(context.Device, vertexShaderByteCode);
            }
        }

        public void Dispose()
        {
            _vertexShader?.Dispose();
        }

        public void Apply(D11RenderContext renderContext)
        {
            renderContext.Context.VertexShader.Set(_vertexShader);
        }
    }
}