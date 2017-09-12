
using SharpDX.Direct3D11;

namespace PersistentPlanet.Graphics.DirectX11
{
    public class D11RenderContext : RenderContext, IRenderContext
    {
        public DeviceContext Context { get; set; }
    }
}