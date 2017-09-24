using System;

namespace PersistentPlanet.Graphics
{
    public interface IMesh : IDisposable
    {
        Vertex[] Vertices { set; }
        uint[] Indices { set; }
    }

    public interface IMesh<in T> : IResource<T>, IMesh
        where T : IRenderContext
    {
    }
}