using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using MemBus;
using MemBus.Configurators;
using SharpDX;
using Buffer = SharpDX.Direct3D11.Buffer;
using Vector4 = System.Numerics.Vector4;

namespace PersistentPlanet
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public readonly Vector3 Position;
        public readonly Vector2 Texture;
        public readonly Vector3 Normal;

        public Vertex(Vector3 position, Vector2 texture, Vector3 normal)
        {
            Position = position;
            Texture = texture;
            Normal = normal;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LightBufferType
    {
        public Vector4 ambientColor;
        public Vector4 diffuseColor;
        public Vector3 lightDirection;
        public float padding;
    };

    public struct HeightMapType
    {
        public float X, Y, Z;
        public float Tu, Tv;
        public float Nx, Ny, Nz;
    };

    public class GameObject : IDisposable
    {
        private readonly ConcurrentDictionary<Type, IComponent> _components = new ConcurrentDictionary<Type, IComponent>();
        private readonly IBus _objectBus;

        public void AddComponent<T>(T component) where T : IComponent
        {
            _components.TryAdd(typeof(T), component);
        }

        public T GetComponent<T>() where T : IComponent
        {
            return (T) (_components.TryGetValue(typeof(T), out IComponent component) ? component : null);
        }

        public GameObject()
        {
            _objectBus = BusSetup.StartWith<Conservative>().Construct();
        }

        public void Initialise(InitialiseContext initialiseContext)
        {
            AddComponent(new Transform(_objectBus) {Position = new Vector3(0, 0, -100)});
            AddComponent(new Terrain.Terrain(_objectBus));

            foreach (var component in _components)
            {
                component.Value.Initialise(initialiseContext);
            }
        }

        public void Dispose()
        {
            foreach (var component in _components)
            {
                component.Value?.Dispose();
            }
        }

        public void Render(IRenderContext renderContext)
        {
            foreach (var component in _components)
            {
                component.Value.Render(renderContext);
            }
        }
    }
}