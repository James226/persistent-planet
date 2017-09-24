using SharpDX;
using Vector3 = System.Numerics.Vector3;

namespace PersistentPlanet.Transform
{
    public class RequestPositionUpdateEvent
    {
        public Vector3 Position { get; set; }
    }
}