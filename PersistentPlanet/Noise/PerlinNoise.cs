using System.Numerics;

namespace PersistentPlanet.Noise
{
    public class Noise
    {
        private static readonly Vector3[] Grad3 =
        {
            new Vector3(1, 1, 0),
            new Vector3(-1, 1, 0),
            new Vector3(1, -1, 0),
            new Vector3(-1, -1, 0),
            new Vector3(1, 0, 1),
            new Vector3(-1, 0, 1),
            new Vector3(1, 0, -1),
            new Vector3(-1, 0, -1),
            new Vector3(0, 1, 1),
            new Vector3(0, -1, 1),
            new Vector3(0, 1, -1),
            new Vector3(0, -1, -1)
        };

        private static readonly int[] Perm = new int[512];

        public static Vector2 To2D(Vector3 vector)
        {
            return new Vector2(vector.X, vector.Y);
        }

        public static float Perlin(float x, float y)
        {
            var i = x > 0 ? (int) x : (int) x - 1;
            var j = y > 0 ? (int) y : (int) y - 1;

            x = x - i;
            y = y - j;

            i = i & 255;
            j = j & 255;

            var gll = Perm[i + Perm[j]] % 12;
            var glh = Perm[i + Perm[j + 1]] % 12;
            var ghl = Perm[i + 1 + Perm[j]] % 12;
            var ghh = Perm[i + 1 + Perm[j + 1]] % 12;

            var nll = Vector2.Dot(To2D(Grad3[gll]), new Vector2(x, y));
            var nlh = Vector2.Dot(To2D(Grad3[glh]), new Vector2(x, y - 1));
            var nhl = Vector2.Dot(To2D(Grad3[ghl]), new Vector2(x - 1, y));
            var nhh = Vector2.Dot(To2D(Grad3[ghh]), new Vector2(x - 1, y - 1));

            var u = (float) (x * x * x * (x * (x * 6 - 15) + 10));
            var v = (float) (y * y * y * (y * (y * 6 - 15) + 10));

            //var nyl = MathF.Lerp(nll, nhl, u);
            var nyl = (1 - u) * nll + u * nhl;
            //var nyh = MathF.Lerp(nlh, nhh, u);
            var nyh = (1 - u) * nlh + u * nhh;

            //var nxy = MathF.Lerp(nyl, nyh, v);
            var nxy = (1 - v) * nyl + v * nyh;

            return nxy;


        }

        public static float Perlin(float x, float y, float z)
        {
            var X = x > 0 ? (int) x : (int) x - 1;
            var Y = y > 0 ? (int) y : (int) y - 1;
            var Z = z > 0 ? (int) z : (int) z - 1;

            x = x - X;
            y = y - Y;
            z = z - Z;

            X = X & 255;
            Y = Y & 255;
            Z = Z & 255;

            var gi000 = Perm[X + Perm[Y + Perm[Z]]] % 12;
            var gi001 = Perm[X + Perm[Y + Perm[Z + 1]]] % 12;
            var gi010 = Perm[X + Perm[Y + 1 + Perm[Z]]] % 12;
            var gi011 = Perm[X + Perm[Y + 1 + Perm[Z + 1]]] % 12;
            var gi100 = Perm[X + 1 + Perm[Y + Perm[Z]]] % 12;
            var gi101 = Perm[X + 1 + Perm[Y + Perm[Z + 1]]] % 12;
            var gi110 = Perm[X + 1 + Perm[Y + 1 + Perm[Z]]] % 12;
            var gi111 = Perm[X + 1 + Perm[Y + 1 + Perm[Z + 1]]] % 12;


            //TODO: inline the dot products to speed up perlin noise
            var n000 = Vector3.Dot(Grad3[gi000], new Vector3(x, y, z));
            var n100 = Vector3.Dot(Grad3[gi100], new Vector3(x - 1, y, z));
            var n010 = Vector3.Dot(Grad3[gi010], new Vector3(x, y - 1, z));
            var n110 = Vector3.Dot(Grad3[gi110], new Vector3(x - 1, y - 1, z));
            var n001 = Vector3.Dot(Grad3[gi001], new Vector3(x, y, z - 1));
            var n101 = Vector3.Dot(Grad3[gi101], new Vector3(x - 1, y, z - 1));
            var n011 = Vector3.Dot(Grad3[gi011], new Vector3(x, y - 1, z - 1));
            var n111 = Vector3.Dot(Grad3[gi111], new Vector3(x - 1, y - 1, z - 1));

            var u = (float) (x * x * x * (x * (x * 6 - 15) + 10));
            var v = (float) (y * y * y * (y * (y * 6 - 15) + 10));
            var w = (float) (z * z * z * (z * (z * 6 - 15) + 10));

            //TODO: inline lerps to speed up perlin noise
            var nx00 = Lerp(n000, n100, u);
            var nx01 = Lerp(n001, n101, u);
            var nx10 = Lerp(n010, n110, u);
            var nx11 = Lerp(n011, n111, u);

            var nxy0 = Lerp(nx00, nx10, v);
            var nxy1 = Lerp(nx01, nx11, v);


            var nxyz = Lerp(nxy0, nxy1, w);

            return nxyz;
        }

        public static float Lerp(float x, float y, float distance)
        {
            var d = y - x;
            return x + (d * distance);
        }
    }
}
