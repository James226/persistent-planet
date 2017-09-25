using System;
using System.Diagnostics;
using System.Numerics;
using MemBus;
using PersistentPlanet.Controls.Controls;
using PersistentPlanet.DualContouring;
using PersistentPlanet.Graphics;
using PersistentPlanet.Graphics.DirectX11;
using PersistentPlanet.Graphics.Vulkan;
using PersistentPlanet.Primitives;

namespace PersistentPlanet
{
    public class Game : IDisposable
    {
        private readonly IRenderWindow _renderWindow;
        private readonly IBus _bus;

        private GameObject _cube;
        private VulkanCamera _camera;
        private bool _running;
        private GameObject _terrain;
        private VulkanRenderer _renderer;
        private Func<VulkanRenderContext> _renderContextGenerator;
        private Scene<VulkanRenderContext> _scene;

        public Game(IRenderWindow renderWindow, IBus bus)
        {
            _renderWindow = renderWindow;
            _bus = bus;
        }

        public void Initialise()
        {
            _running = true;
            _bus.Subscribe<EscapePressedEvent>(_ => _running = false);

            _renderer = new VulkanRenderer();
            (var initialiseContext, var renderContextGenerator) = _renderer.Initialise(_renderWindow, _bus);
            _renderContextGenerator = renderContextGenerator;

            _scene = _renderer.CreateScene();
            _cube = new GameObject();
            _cube.AddComponent<Cube>();
            _cube.AddComponent<CubeController>();
            _cube.Initialise(initialiseContext, _scene);
            _cube.GetComponent<Transform.Transform>().Position = new Vector3(110, 7, 30);

            _terrain = new GameObject();
            _terrain.AddComponent<VoxelObject>();
            _terrain.Initialise(initialiseContext, _scene);

            _camera = new VulkanCamera();
            _camera.Initialise(initialiseContext);
        }

        public void Dispose()
        {
            _terrain?.Dispose();
            _cube?.Dispose();
            _bus?.Dispose();
        }

        public void Run()
        {
            var sw = new Stopwatch();
            sw.Start();
            var frame = 0;

            Win32.QueryPerformanceFrequency(out var counterFrequency);

            Win32.QueryPerformanceCounter(out var lastTimestamp);
            var lastFps = lastTimestamp;

            while (_running && _renderWindow.NextFrame())
            {
                Win32.QueryPerformanceCounter(out var timestamp);

                var renderContext = _renderContextGenerator.Invoke();

                _camera.Apply(renderContext);

                _renderer.Render(renderContext,
                                 () =>
                                 {

                                     //_camera.Apply(renderContext);
                                     _scene.Render(renderContext);
                                 });

                frame++;

                if (timestamp - lastFps <= counterFrequency) continue;
                Console.WriteLine(frame);
                lastFps = timestamp;
                frame = 0;
            }

            sw.Stop();
        }
    }
}