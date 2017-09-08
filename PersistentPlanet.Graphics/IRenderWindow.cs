using System;

namespace PersistentPlanet.Graphics
{
    public interface IRenderWindow
    {
        int WindowWidth { get; }
        int WindowHeight { get; }
        IntPtr Handle { get; }
        bool NextFrame();
    }
}