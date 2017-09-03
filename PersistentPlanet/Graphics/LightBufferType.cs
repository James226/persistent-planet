using System.Numerics;
using System.Runtime.InteropServices;
using Vector3 = SharpDX.Vector3;

namespace PersistentPlanet.Graphics
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LightBufferType
    {
        public Vector4 ambientColor;
        public Vector4 diffuseColor;
        public Vector3 lightDirection;
        public float padding;
    };
}