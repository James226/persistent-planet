using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using MemBus;
using PersistentPlanet.Graphics;

namespace PersistentPlanet.DualContouring
{
    public class VoxelObject : IComponent
    {
        private OctreeNode _root;

        public IMaterial Material;
        public List<DensityModifier> Modifiers = new List<DensityModifier>(); 

        private bool _initialised;
        private bool _generated;
        private Thread _thread;
        private static Dictionary<float, Dictionary<float, Dictionary<float, float>>> _densityCache;
        private static Dictionary<float, Dictionary<float, Dictionary<float, float>>> _lastDensityCache;

        private int _lastModifierCount;
        private bool _firstBuild = true;

        public bool Building => !_generated;

        public void Start ()
        {
            if (_initialised) return;
            QueueRebuild();
        }

        private void Build(Vector3? position = null)
        {
            var sw = new Stopwatch();
            sw.Start();
            Octree.Leaf = Octree.NormalCount = Octree.CrossingPosition = 0;
            const int octreeSize = 64;
            _cacheHit = _cacheMiss = 0;
            Octree.sw = new Stopwatch();
            _lastDensityCache = _densityCache ?? new Dictionary<float, Dictionary<float, Dictionary<float, float>>>();
            _densityCache = new Dictionary<float, Dictionary<float, Dictionary<float, float>>>();
            var threshold = -0.1f;

            if (position == null)
            {
                _root = Octree.BuildOctree(new Vector3(-octreeSize / 2f, -octreeSize / 2f, -octreeSize / 2f), octreeSize,
                                           threshold, Density_Func);
            }
            else
            {
                _root = Octree.BuildOctree(new Vector3(-octreeSize / 2f, -octreeSize / 2f, -octreeSize / 2f), octreeSize,
                                           threshold, Density_Func);
            }

            if (_root == null)
                Debug.WriteLine("root is null");

            _initialised = true;
            sw.Stop();
            Debug.WriteLine("Leaf time in: " + Octree.sw.Elapsed);
            Debug.WriteLine("Build Complete in: " + sw.Elapsed);
        }

        public void Update ()
        {
            if (_initialised && !_generated)
            {
                //Generate();
            }
        }

        private void Generate(IResourceCollection resourceCollection)
        {
            Debug.WriteLine("Miss: " + _cacheMiss + ", Hit: " + _cacheHit + ", Count: " + _lastModifierCount);
            Debug.WriteLine("Crossing: " + Octree.CrossingPosition + ", Normal: " + Octree.NormalCount + ", Leaf: " + Octree.Leaf);

            var c = _densityCache.Sum(x => x.Value.Sum(y => y.Value.Count));
            Debug.WriteLine("Cache Size: " + c);
            _lastModifierCount = Modifiers.Count;
            _firstBuild = false;
            Debug.WriteLine("Generating voxel object:");
            _generated = true;

            void BuildMesh(Vertex[] vertices, uint[] indices)
            {
                _mesh = resourceCollection.CreateMesh(ObjectBus, vertices, indices);
            }

            Octree.GenerateMeshFromOctree(_root, BuildMesh);
        }
    
        public void QueueRebuild(Vector3? position = null, Vector3? size = null)
        {
            _initialised = false;
            _generated = false;
            _thread = new Thread(() => Build(position));
            _thread.Start();
        }

        public void SyncRebuild(IResourceCollection resourceCollection)
        {
            Build();
            Generate(resourceCollection);
        }

        public void OnDestroy()
        {
            Debug.WriteLine("Destroying: ");
            _initialised = false;
            _generated = false;

            if (_thread != null)
            {
                _thread.Abort();
                _thread.Join();
            }

            Octree.DestroyOctree(_root);
        }

        private float Density_Func(Vector3 worldPosition)
        {
            var lastDensity = GetCachedDensity(worldPosition);
            return CalculateDensity(worldPosition, lastDensity ?? 0, _firstBuild || !lastDensity.HasValue);
        }

        private static int _cacheHit, _cacheMiss;
        private IMesh _mesh;

        private float? GetCachedDensity(Vector3 worldPosition)
        {
            if (_lastDensityCache.TryGetValue(worldPosition.X, out var x))
            {
                if (x.TryGetValue(worldPosition.Y, out var y))
                {
                    if (y.TryGetValue(worldPosition.Z, out var d))
                    {
                        _cacheHit++;
                        return d;
                    }
                }
            }
            _cacheMiss++;
            return null;
        }

        private float CalculateDensity(Vector3 worldPosition, float lastDensity, bool firstBuild)
        {
            var density = lastDensity;
            if (firstBuild)
            {
                var MAX_HEIGHT = 20.0f;
                //var noise = FractalNoise(4, 0.5343f, 2.2324f, 0.68324f, new Vector2(worldPosition.X, worldPosition.Z));
                var terrain = worldPosition.Y - (MAX_HEIGHT * 0.6f); // noise);

                //float cube = Cuboid(worldPosition, new Vector3(-4.0f, 10.0f, -4.0f), new Vector3(12.0f, 12.0f, 12.0f));
                var cube = Cuboid(worldPosition, new Vector3(-4.0f, 10.0f, -4.0f), new Vector3(5, 5, 5));

                var sphere = Sphere(worldPosition, new Vector3(15.0f, 1.5f, 1.0f), 16.0f);

                density = MathF.Min(sphere, terrain);
                density = MathF.Max(-cube, density);
            }

            for (var index = firstBuild ? 0 : _lastModifierCount; index < Modifiers.Count; index++)
            {
                var m = Modifiers[index];
                float d;
                switch (m.Tool)
                {
                    case Tool.Cube:
                        d = Cuboid(worldPosition, m.Position, new Vector3(m.Size, m.Size, m.Size));
                        break;
                    case Tool.Sphere:
                        d = Sphere(worldPosition, m.Position, m.Size);
                        break;
                    default:
                        d = -1;
                        break;
                }
                density = m.Additive ? MathF.Min(d, density) : MathF.Max(-d, density);
            }

            if (!_densityCache.TryGetValue(worldPosition.X, out var x))
            {
                x = new Dictionary<float, Dictionary<float, float>>();
                _densityCache[worldPosition.X] = x;
            }

            if (!x.TryGetValue(worldPosition.Y, out var y))
            {
                y = new Dictionary<float, float>();
                x[worldPosition.Y] = y;
            }

            y[worldPosition.Z] = density;
            return density;
        }

        private static float Sphere(Vector3 worldPosition, Vector3 origin, float radius)
        {
            return (worldPosition - origin).Length() - radius;
        }

        private static float Cuboid(Vector3 worldPosition, Vector3 origin, Vector3 halfDimensions)
        {
            //return (origin.x > worldPosition.x - halfDimensions.x && origin.x < worldPosition.x + halfDimensions.x &&
            //        origin.y > worldPosition.y - halfDimensions.y && origin.y < worldPosition.y + halfDimensions.y &&
            //        origin.z > worldPosition.z - halfDimensions.z && origin.z < worldPosition.z + halfDimensions.z)
            //    ? 1
            //    : -1;

            var localPos = worldPosition - origin;
            var pos = localPos;

            var d = new Vector3(MathF.Abs(pos.X), MathF.Abs(pos.Y), MathF.Abs(pos.Z)) - halfDimensions;
            var m = MathF.Max(d.X, MathF.Max(d.Y, d.Z));
            return MathF.Min(m, (d.Length() > 0 ? d : Vector3.Zero).Length());
        }

        public static float FractalNoise(int octaves, float frequency, float lacunarity, float persistence, Vector2 position)
        {
            var SCALE = 1.0f / 128.0f;
            var p = position * SCALE;
            var noise = 0.0f;

            var amplitude = 1.0f;
            p *= frequency;

            for (var i = 0; i < octaves; i++)
            {
                noise += Noise.Noise.Perlin(p.X, p.Y) * amplitude;
                p *= lacunarity;
                amplitude *= persistence;
            }

            // move into [0, 1] range
            return 0.5f + (0.5f * noise);
        }

        public void Initialise(IInitialiseContext context, IResourceCollection resourceCollection)
        {
            SyncRebuild(resourceCollection);
        }

        public void Dispose()
        {
            _mesh?.Dispose();
        }

        public IBus ObjectBus { get; set; }
    }

    public enum Tool
    {
        Sphere,
        Cube
    }

    [Serializable]
    public class DensityModifier
    {
        public Tool Tool;
        public bool Additive;
        public Vector3 Position;
        public int Size = 1;
    }
}