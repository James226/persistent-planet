using System;

namespace PersistentPlanet.Graphics
{
    public interface IMesh : IDisposable
    {
        void SetMesh(Vertex[] vertices, uint[] indices);
        Vertex[] Vertices { set; }
        uint[] Indices { set; }
    }

    public interface IMesh<in T> : IResource<T>, IMesh
        where T : IRenderContext
    {
    }
}