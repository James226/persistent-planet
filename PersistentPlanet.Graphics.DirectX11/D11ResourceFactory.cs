using MemBus;

namespace PersistentPlanet.Graphics.DirectX11
{
    public class D11ResourceFactory : IResourceFactory<D11RenderContext>
    {
        private readonly D11InitialiseContext _context;

        public D11ResourceFactory(D11InitialiseContext context)
        {
            _context = context;
        }

        public IMaterial<D11RenderContext> CreateMaterial(IBus objectBus)
        {
            var material = new Material(objectBus);
            material.Initialise(_context);
            return material;
        }

        public IMesh<D11RenderContext> CreateMesh(Vertex[] vertices, int[] indices)
        {
            var mesh = new Mesh();
            mesh.Initialise(_context, vertices, indices);
            return mesh;
        }
    }
}