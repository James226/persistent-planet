using System.Numerics;

namespace PersistentPlanet.DualContouring
{
    public class OctreeDrawInfo 
    {
        public uint index;
        public int corners;
        public Vector3 position;
        public Vector3 averageNormal;
        public QefData	qef;
    }
}
