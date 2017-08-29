using System;
using MemBus;

namespace PersistentPlanet
{
    public class Material : IDisposable
    {
        private IShader _pixelShader;
        private IShader _vertexShader;
        private readonly Func<string, string, IShader> _pixelShaderFactory;
        private readonly Func<string, string, IShader> _vertexShaderFactory;

        public Material(IBus objectBus)
            : this((file, func) => new PixelShader(file, func), (file, func) => new VertexShader(objectBus, file, func))
        {
        }

        public Material(Func<string, string, IShader> pixelShaderFactory, Func<string, string, IShader> vertexShaderFactory)
        {
            _pixelShaderFactory = pixelShaderFactory;
            _vertexShaderFactory = vertexShaderFactory;
        }

        public void Initialise(InitialiseContext context)
        {
            _pixelShader = _pixelShaderFactory.Invoke("pixelShader.hlsl", "main");
            _pixelShader.Initialise(context);

            _vertexShader = _vertexShaderFactory.Invoke("vertexShader.hlsl", "main");
            _vertexShader.Initialise(context);
        }

        public void Dispose()
        {
            _vertexShader.Dispose();
            _pixelShader.Dispose();
        }

        public void Render(IRenderContext context)
        {
            _vertexShader.Apply(context);
            _pixelShader.Apply(context);
        }
    }
}
