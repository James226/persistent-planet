using MemBus;

namespace PersistentPlanet.Graphics
{ 
    public class InitialiseContext : IInitialiseContext
    {
        public IRenderWindow RenderWindow { get; set; }
        public IBus Bus { get; set; }
    }
}