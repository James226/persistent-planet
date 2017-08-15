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
            var renderWindow = new RenderWindow(AppName, ClassName, WindowWidth, WindowHeight);
            renderWindow.Create();

            var game = new Game(renderWindow);
            game.Initialise();

            game.Run();
        }
    }
}