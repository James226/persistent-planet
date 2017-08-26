using SharpDX.Direct3D11;

namespace PersistentPlanet
{
    public interface IRenderContext
    {
        DeviceContext Context { get; }
    }

    public class RenderContext : IRenderContext
    {
        public DeviceContext Context { get; set; }
    }
}