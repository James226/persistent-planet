using System;
using MemBus;

namespace PersistentPlanet.Graphics.Vulkan
{
    public class VulkanResourceFactory : IResourceFactory<VulkanRenderContext>
    {
        private readonly VulkanInitialiseContext _context;

        public VulkanResourceFactory(VulkanInitialiseContext context)
        {
            _context = context;
        }

        public IMaterial<VulkanRenderContext> CreateMaterial(IBus objectBus)
        {
            //throw new NotImplementedException();
            return null;
        }

        public IMesh<VulkanRenderContext> CreateMesh(Vertex[] vertices, uint[] indices)
        {
            var mesh = new VulkanMesh();
            mesh.Initialise(_context, vertices, indices);
            return mesh;
        }
    }
}