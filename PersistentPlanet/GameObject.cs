using System;
using System.Collections.Concurrent;
using MemBus;
using MemBus.Configurators;
using PersistentPlanet.Graphics;
using PersistentPlanet.Graphics.DirectX11;
using PersistentPlanet.Graphics.Vulkan;

namespace PersistentPlanet
{
    public class GameObject : IDisposable
    {
        private readonly ConcurrentDictionary<Type, IComponent> _components = new ConcurrentDictionary<Type, IComponent>();
        private readonly IBus _objectBus;
        private IResourceCollection _resourceCollection;

        public void AddComponent<T>() where T : IComponent, new()
        {
            var component = new T();
            AddComponent(component);
        }

        private void AddComponent<T>(T component) where T : IComponent
        {
            component.ObjectBus = _objectBus;
            _components.TryAdd(typeof(T), component);
        }

        public T GetComponent<T>() where T : IComponent
        {
            return (T) (_components.TryGetValue(typeof(T), out var component) ? component : null);
        }

        public GameObject()
        {
            _objectBus = BusSetup.StartWith<Conservative>().Construct();
            
            AddComponent<Transform.Transform>();
        }

        public void Initialise(VulkanInitialiseContext initialiseContext, IScene scene)
        {
            _resourceCollection = scene.CreateResourceCollection();
            foreach (var component in _components)
            {
                component.Value.Initialise(initialiseContext, _resourceCollection);
            }
        }

        public void Dispose()
        {
            foreach (var component in _components)
            {
                component.Value?.Dispose();
            }
        }
    }
}