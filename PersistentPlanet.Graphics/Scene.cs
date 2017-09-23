using System;
using System.Collections.Generic;

namespace PersistentPlanet.Graphics
{
    public interface IScene
    {
        IResourceCollection CreateResourceCollection();
    }

    public class Scene<T> : IScene where T : IRenderContext
    {
        private readonly IResourceFactory<T> _resourceFactory;
        private readonly List<ResourceCollection<T>> _resourceCollections = new List<ResourceCollection<T>>();

        public Scene(IResourceFactory<T> resourceFactory)
        {
            _resourceFactory = resourceFactory;
        }

        public IResourceCollection CreateResourceCollection()
        {
            var resourceCollection = new ResourceCollection<T>(_resourceFactory);
            _resourceCollections.Add(resourceCollection);
            return resourceCollection;
        }
        
        public void Render(T context)
        {
            foreach (var resourceCollection in _resourceCollections)
            foreach (var resource in resourceCollection)
            {
                resource?.Render(context);
            }
        }
    }
}
