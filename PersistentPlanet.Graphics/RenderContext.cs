using MemBus;

namespace PersistentPlanet.Graphics
{
    public class RenderContext : IRenderContext
    {
        public IBus Bus { get; set; }
        public float DeltaTime { get; set; }
    }
}