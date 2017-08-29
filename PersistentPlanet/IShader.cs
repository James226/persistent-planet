using System;
using MemBus;

namespace PersistentPlanet
{
    public interface IShader : IDisposable
    {
        void Initialise(IInitialiseContext context);
        void Apply(IRenderContext renderContext);
    }
}