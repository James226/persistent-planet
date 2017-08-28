using MemBus;
using SharpDX.Direct3D11;

namespace PersistentPlanet
{
    public interface IInitialiseContext
    {
        Device Device { get; }
        IRenderWindow RenderWindow { get; }
        IBus Bus { get; }
    }

    public class InitialiseContext : IInitialiseContext
    {
        public Device Device { get; set; }
        public IRenderWindow RenderWindow { get; set; }
        public IBus Bus { get; set; }
    }
}