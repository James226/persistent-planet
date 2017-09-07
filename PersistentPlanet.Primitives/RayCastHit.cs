﻿using System.Numerics;

namespace PersistentPlanet.Primitives
{
    public struct RayCastHit<T>
    {
        public readonly T Item;
        public readonly Vector3 Location;
        public readonly float Distance;

        public RayCastHit(T item, Vector3 location, float distance)
        {
            Item = item;
            Location = location;
            Distance = distance;
        }
    }
}
