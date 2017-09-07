using MemBus;
using SharpDX.Direct3D11;

namespace PersistentPlanet.Graphics
{
    public interface IRenderContext
    {
        DeviceContext Context { get; }
    }

    public class RenderContext : IRenderContext
    {
        public DeviceContext Context { get; set; }
        public IBus Bus { get; set; }
        public float DeltaTime { get; set; }
    }
}