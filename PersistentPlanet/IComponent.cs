using System;
using MemBus;

namespace PersistentPlanet
{
    public interface IComponent : IDisposable
    {
        IBus ObjectBus { get; set; }
        void Initialise(InitialiseContext context);
        void Render(IRenderContext context);
    }
}
