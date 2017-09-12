using System;
using MemBus;
using PersistentPlanet.Graphics;
using PersistentPlanet.Graphics.DirectX11;

namespace PersistentPlanet
{
    public interface IComponent : IDisposable
    {
        IBus ObjectBus { get; set; }
        void Initialise(IInitialiseContext context, IResourceCollection resourceCollection);
    }
}
