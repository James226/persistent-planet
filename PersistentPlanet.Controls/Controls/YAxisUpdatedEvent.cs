using System.Numerics;

namespace PersistentPlanet.Controls.Controls
{
    public class YAxisUpdatedEvent : IAxisUpdatedEvent
    {
        public Vector2 Axis { get; set; }
    }
}