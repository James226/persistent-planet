using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace PersistentPlanet.Graphics.DirectX11
{
    public class Mesh : IMesh<D11RenderContext>
    {
        private int _size;
        private Buffer _indexBuffer;
        private Buffer _vertexBuffer;

        public void Initialise(D11InitialiseContext context, Vertex[] vertices, uint[] indices)
        {
            _size = indices.Length;
            _vertexBuffer = Buffer.Create(context.Device, BindFlags.VertexBuffer, vertices);
            _indexBuffer = Buffer.Create(context.Device, BindFlags.IndexBuffer, indices);
        }

        public void Dispose()
        {
            _indexBuffer?.Dispose();
            _vertexBuffer?.Dispose();
        }

        public void Render(D11RenderContext renderContext)
        {
            renderContext.Context.InputAssembler.SetVertexBuffers(0,
                                                                  new VertexBufferBinding(
                                                                      _vertexBuffer,
                                                                      Utilities.SizeOf<Vertex>(),
                                                                      0));

            renderContext.Context.InputAssembler.SetIndexBuffer(_indexBuffer, Format.R32_UInt, 0);

            renderContext.Context.DrawIndexed(_size, 0, 0);
        }
    }
}
