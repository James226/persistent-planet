using MemBus;
using SharpDX.Direct3D11;
using SharpDX.DirectInput;

namespace PersistentPlanet
{
    public interface IRenderContext
    {
        DeviceContext Context { get; }
        Input Input { get; }
    }

    public class RenderContext : IRenderContext
    {
        public DeviceContext Context { get; set; }
        public Input Input { get; set; }
        public IBus Bus { get; set; }
    }
}