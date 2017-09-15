using MemBus;

namespace PersistentPlanet.Graphics.Vulkan
{
    public class VulkanInitialiseContext : IInitialiseContext
    {
        public IBus Bus { get; set; }
        public VulkanContext Context { get; set; }
    }
}
