using MemBus;

namespace PersistentPlanet.Graphics.Vulkan
{
    public class VulkanRenderContext : IRenderContext
    {
        public IBus Bus { get; set; }
        public float DeltaTime { get; set; }
    }
}