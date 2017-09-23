using MemBus;
using VulkanCore;

namespace PersistentPlanet.Graphics.Vulkan
{
    public class VulkanInitialiseContext : IInitialiseContext
    {
        public IBus Bus { get; set; }
        public VulkanContext Context { get; set; }
        public ContentManager Content { get; set; }
        public IRenderWindow RenderWindow { get; set; }
        public RenderPass RenderPass { get; set; }

        public VulkanBuffer WorldBuffer { get; set; }
    }
}
