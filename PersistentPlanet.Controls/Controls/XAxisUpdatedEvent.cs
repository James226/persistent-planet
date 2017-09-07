using System.Numerics;

namespace PersistentPlanet.Controls.Controls
{
    public class XAxisUpdatedEvent : IAxisUpdatedEvent
    {
        public Vector2 Axis { get; set; }
    }
}