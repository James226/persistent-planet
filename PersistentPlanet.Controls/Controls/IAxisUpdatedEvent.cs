using System.Numerics;

namespace PersistentPlanet.Controls.Controls
{
    public interface IAxisUpdatedEvent
    {
        Vector2 Axis { get; set; }
    }
}