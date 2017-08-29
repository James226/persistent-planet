using System;
using MemBus;

namespace PersistentPlanet
{
    public interface IComponent : IDisposable
    {
        void Initialise(InitialiseContext context);
        void Render(IRenderContext context);
    }
}
