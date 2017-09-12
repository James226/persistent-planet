using System;

namespace PersistentPlanet.Graphics
{
    public interface IMesh : IDisposable
    {
    }

    public interface IMesh<in T> : IResource<T>, IMesh
        where T : IRenderContext
    {
    }
}