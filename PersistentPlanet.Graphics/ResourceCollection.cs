using System;
using System.Collections;
using System.Collections.Generic;
using MemBus;

namespace PersistentPlanet.Graphics
{
    public interface IMaterial : IDisposable
    {
    }

    public interface IMaterial<in TRenderContext> : IMaterial, IResource<TRenderContext>
        where TRenderContext : IRenderContext
    {
    }

    public interface IResourceFactory<in TRenderContext>
        where TRenderContext : IRenderContext
    {
        IMaterial<TRenderContext> CreateMaterial(IBus objectBus);
        IMesh<TRenderContext> CreateMesh(Vertex[] vertices, uint[] indices);
    }

    public interface IResourceCollection
    {
        IMaterial CreateMaterial(IBus objectBus);
        IMesh CreateMesh(Vertex[] vertices, uint[] indices);
    }

    public class ResourceCollection<TRenderContext> : IEnumerable<IResource<TRenderContext>>, IResourceCollection
        where TRenderContext : IRenderContext
    {
        private readonly IResourceFactory<TRenderContext> _resourceFactory;
        private readonly List<IResource<TRenderContext>> _items;

        public ResourceCollection(IResourceFactory<TRenderContext> resourceFactory)
        {
            _resourceFactory = resourceFactory;
            _items = new List<IResource<TRenderContext>>();
        }

        public IMaterial CreateMaterial(IBus objectBus)
        {
            var vertexShader = _resourceFactory.CreateMaterial(objectBus);
            _items.Add(vertexShader);
            return vertexShader;
        }

        public IMesh CreateMesh(Vertex[] vertices, uint[] indices)
        {
            var mesh = _resourceFactory.CreateMesh(vertices, indices);
            _items.Add(mesh);
            return mesh;
        }

        public IEnumerator<IResource<TRenderContext>> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}