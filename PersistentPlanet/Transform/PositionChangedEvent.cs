using SharpDX;
using Vector3 = System.Numerics.Vector3;

namespace PersistentPlanet.Transform
{
    public class PositionChangedEvent
    {
        public Vector3 Position { get; set; }
    }
}