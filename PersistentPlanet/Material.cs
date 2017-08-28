using System;
using SharpDX.Direct3D;

namespace PersistentPlanet
{
    public class Material : IDisposable
    {
        private PixelShader _pixelShader;
        private VertexShader _vertexShader;

        public void Initialise(InitialiseContext context)
        {
            _pixelShader = new PixelShader("pixelShader.hlsl", "main");
            _pixelShader.Initialise(context);

            _vertexShader = new VertexShader("vertexShader.hlsl", "main");
            _vertexShader.Initialise(context);
        }

        public void Dispose()
        {
            _vertexShader.Dispose();
            _pixelShader.Dispose();
        }

        public void Render(RenderContext context, GameObject gameObject)
        {
            _vertexShader.Apply(context, gameObject);
            _pixelShader.Apply(context, gameObject);
            context.Context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
        }
    }
}
