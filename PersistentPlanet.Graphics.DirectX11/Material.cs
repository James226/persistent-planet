using System;
using MemBus;

namespace PersistentPlanet.Graphics.DirectX11
{
    public class Material : IMaterial<D11RenderContext>, IDisposable
    {
        public string PixelShaderFilename { get; set; } = "pixelShader.hlsl";
        public string PixelShaderFunction { get; set; } = "main";
        public string VertexShaderFilename { get; set; } = "vertexShader.hlsl";
        public string VertexShaderFunction { get; set; } = "main";

        private IShader _pixelShader;
        private IShader _vertexShader;
        private readonly Func<string, string, IShader> _pixelShaderFactory;
        private readonly Func<string, string, IShader> _vertexShaderFactory;

        public Material(IBus objectBus)
            : this((file, func) => new TexturePixelShader(file, func), (file, func) => new StandardVertexShader(objectBus, file, func))
        {
        }

        public Material(Func<string, string, IShader> pixelShaderFactory, Func<string, string, IShader> vertexShaderFactory)
        {
            _pixelShaderFactory = pixelShaderFactory;
            _vertexShaderFactory = vertexShaderFactory;
        }

        public void Initialise(D11InitialiseContext context)
        {
            _pixelShader = _pixelShaderFactory.Invoke(PixelShaderFilename, PixelShaderFunction);
            _pixelShader.Initialise(context);

            _vertexShader = _vertexShaderFactory.Invoke(VertexShaderFilename, VertexShaderFunction);
            _vertexShader.Initialise(context);
        }

        public void Dispose()
        {
            _vertexShader.Dispose();
            _pixelShader.Dispose();
        }

        public void Render(D11RenderContext context)
        {
            _vertexShader.Apply(context);
            _pixelShader.Apply(context);
        }
    }
}
