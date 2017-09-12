using System;

namespace PersistentPlanet.Graphics
{
    public interface IResource<in T> : IDisposable where T : IRenderContext
    {
        void Render(T context);
    }
}