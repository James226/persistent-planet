using System;
using System.Numerics;

namespace PersistentPlanet.DualContouring
{
    public static class glm
    {
        public static float Sphere(Vector3 worldPosition, Vector3 origin, float radius)
        {
            return (worldPosition - origin).Length() - radius;
        }

        public static float Cuboid(Vector3 worldPosition, Vector3 origin, Vector3 halfDimensions)
        {
            Vector3 local_pos = worldPosition - origin;
            Vector3 pos = local_pos;

            Vector3 d = new Vector3(MathF.Abs(pos.X), MathF.Abs(pos.Y), MathF.Abs(pos.Z)) - halfDimensions;
            float m = MathF.Max(d.X, MathF.Max(d.Y, d.Z));
            return MathF.Min(m, (d.Length() > 0 ? d : Vector3.Zero).Length());
        }

        public static float FractalNoise(int octaves, float frequency, float lacunarity, float persistence, Vector2 position)
        {
            float SCALE = 1.0f / 128.0f;
            Vector2 p = position * SCALE;
            float noise = 0.0f;

            float amplitude = 1.0f;
            p *= frequency;

            for (int i = 0; i < octaves; i++)
            {
                noise += Noise.Noise.Perlin(p.X, p.Y) * amplitude;
                p *= lacunarity;
                amplitude *= persistence;
            }

            // move into [0, 1] range
            return 0.5f + (0.5f * noise);
        }


        public static float Density_Func(Vector3 worldPosition)
        {
            float MAX_HEIGHT = 20.0f;
            //float noise = FractalNoise(4, 0.5343f, 2.2324f, 0.68324f, new Vector2(worldPosition.x, worldPosition.z));
            float terrain = worldPosition.Y - (MAX_HEIGHT*0.6f); //noise);

            //float cube = Cuboid(worldPosition, new Vector3(-4.0f, 10.0f, -4.0f), new Vector3(12.0f, 12.0f, 12.0f));
            float cube = Cuboid(worldPosition, new Vector3(-4.0f, 10.0f, -4.0f), new Vector3(5, 5, 5));
            float sphere = Sphere(worldPosition, new Vector3(15.0f, 2.5f, 1.0f), 16.0f);

            return MathF.Min(cube, MathF.Min(sphere, terrain));
        }
    }
}