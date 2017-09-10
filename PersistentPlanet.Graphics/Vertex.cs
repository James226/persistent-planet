using System.Runtime.InteropServices;
using SharpDX;

namespace PersistentPlanet.Graphics
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public readonly Vector3 Position;
        public readonly Vector2 Texture;
        public readonly Vector3 Normal;

        public Vertex(Vector3 position, Vector2 texture, Vector3 normal)
        {
            Position = position;
            Texture = texture;
            Normal = normal;
        }
    }
}