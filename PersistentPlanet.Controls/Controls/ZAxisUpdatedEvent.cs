using System.Numerics;

namespace PersistentPlanet.Controls.Controls
{
    public class ZAxisUpdatedEvent : IAxisUpdatedEvent
    {
        public Vector2 Axis { get; set; }
    }
}