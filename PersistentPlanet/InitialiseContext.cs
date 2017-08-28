using MemBus;
using SharpDX;
using SharpDX.Direct3D11;

namespace PersistentPlanet
{
    public interface IInitialiseContext
    {
        Device Device { get; }
        Vector2 WindowSize { get; }
    }

    public class InitialiseContext : IInitialiseContext
    {
        public Device Device { get; set; }
        public Vector2 WindowSize { get; set; }
        public IRenderWindow RenderWindow { get; set; }
        public IBus Bus { get; set; }
    }
}