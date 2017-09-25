using System.Threading;
using MemBus;
using VulkanCore;

namespace PersistentPlanet.Graphics.Vulkan
{
    public class VulkanRenderContext : IRenderContext
    {
        public IBus Bus { get; set; }
        public float DeltaTime { get; set; }
        public CommandBuffer CommandBuffer { get; set; }
        public CountdownLatch RenderWait { get; set; }
    }
}