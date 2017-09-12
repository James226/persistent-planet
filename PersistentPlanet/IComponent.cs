using System;
using MemBus;
using PersistentPlanet.Graphics;

namespace PersistentPlanet
{
    public interface IComponent : IDisposable
    {
        IBus ObjectBus { get; set; }
        void Initialise(IInitialiseContext context, IResourceCollection resourceCollection);
    }
}
