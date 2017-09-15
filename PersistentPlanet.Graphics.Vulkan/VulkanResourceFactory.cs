using System;
using MemBus;

namespace PersistentPlanet.Graphics.Vulkan
{
    public class VulkanResourceFactory : IResourceFactory<VulkanRenderContext>
    {
        public IMaterial<VulkanRenderContext> CreateMaterial(IBus objectBus)
        {
            throw new NotImplementedException();
        }

        public IMesh<VulkanRenderContext> CreateMesh(Vertex[] vertices, uint[] indices)
        {
            throw new NotImplementedException();
        }
    }
}