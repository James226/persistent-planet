using MemBus;
using MemBus.Configurators;

namespace PersistentPlanet
{
    public static class Program
    {
        private const int WindowWidth = 1280;
        private const int WindowHeight = 720;
        private const string AppName = "Persistent Planet";
        private const string ClassName = "PersistentPlanet";

        public static void Main()
        {
            var bus = BusSetup.StartWith<Conservative>().Construct();

            //var renderWindow = new RenderWindow(AppName, ClassName, WindowWidth, WindowHeight, bus);
            var renderWindow = new SdlWindow(AppName, ClassName, WindowWidth, WindowHeight, bus);
            renderWindow.Create();

            using (var game = new Game(renderWindow, bus))
            {
                game.Initialise();
                game.Run();
            }
        }
    }
}