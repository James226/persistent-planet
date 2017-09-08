using MemBus;

namespace PersistentPlanet.Graphics
{
    public interface IRenderContext
    {
        IBus Bus { get; }
        float DeltaTime { get; }
    }
}