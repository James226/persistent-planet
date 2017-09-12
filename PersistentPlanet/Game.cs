using System;
using System.Diagnostics;
using MemBus;
using PersistentPlanet.Controls.Controls;
using PersistentPlanet.Graphics;
using PersistentPlanet.Graphics.DirectX11;
using PersistentPlanet.Primitives;
using SharpDX;

namespace PersistentPlanet
{
    public class Game : IDisposable
    {
        private readonly IRenderWindow _renderWindow;
        private readonly IBus _bus;

        private GameObject _cube;
        private Camera _camera;
        private bool _running;
        private GameObject _terrain;
        private D11Renderer _renderer;
        private Func<D11RenderContext> _renderContextGenerator;
        private Scene<D11RenderContext> _scene;
        private Material _material;

        public Game(IRenderWindow renderWindow, IBus bus)
        {
            _renderWindow = renderWindow;
            _bus = bus;
        }

        public void Initialise()
        {
            _running = true;
            _bus.Subscribe<EscapePressedEvent>(_ => _running = false);

            _renderer = new D11Renderer();
            (var initialiseContext, var renderContextGenerator) = _renderer.Initialise(_renderWindow, _bus);
            _renderContextGenerator = renderContextGenerator;

            _scene = _renderer.CreateScene();
            _cube = new GameObject();
            _cube.AddComponent<Cube>();
            _cube.AddComponent<CubeController>();
            _cube.Initialise(initialiseContext, _scene);
            _cube.GetComponent<Transform.Transform>().Position = new Vector3(110, 7, 30);

            _terrain = new GameObject();
            _terrain.AddComponent<Terrain.Terrain>();
            _terrain.Initialise(initialiseContext, _scene);

            _camera = new Camera();
            _camera.Initialise(initialiseContext);
        }

        public void Dispose()
        {
            _terrain?.Dispose();
            _cube?.Dispose();
            _material?.Dispose();
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

                _renderer.Render(renderContext,
                                 () =>
                                 {
                                     _camera.Apply(renderContext);
                                     //_material.Render(renderContext);
                                     _scene.Render(renderContext);

                                     //_cube.Render(renderContext);
                                     //_terrain.Render(renderContext);
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