using System;
using System.Numerics;
using MemBus;
using PersistentPlanet.Graphics;
using PersistentPlanet.Graphics.DirectX11;

namespace PersistentPlanet.Primitives
{
    public class Cube : IComponent
    {
        public IBus ObjectBus { get; set; }

        private IMesh _mesh;
        private IMaterial _material;

        public void Initialise(IInitialiseContext context, IResourceCollection resourceCollection)
        {
            var height = 5 * .5f;
            var width = 5 * .5f;
            var length = 5 * .5f;

            var halfPi = (float) Math.PI / 2f;
            var vertices = new[]
            {
                new Vertex(new Vector3(-width, height, length), new Vector2(0, 0), new Vector3(-halfPi, halfPi, halfPi)), 
                new Vertex(new Vector3(width, height, length), new Vector2(1, 0), new Vector3(halfPi, halfPi, halfPi)), 
                new Vertex(new Vector3(width, height, -length), new Vector2(1, 1), new Vector3(halfPi, halfPi, -halfPi)), 
                new Vertex(new Vector3(-width, height, -length), new Vector2(0, 1), new Vector3(-halfPi, halfPi, -halfPi)), 

                new Vertex(new Vector3(-width, -height, length), new Vector2(1, 1), new Vector3(-halfPi, -halfPi, halfPi)), 
                new Vertex(new Vector3(width, -height, length), new Vector2(0, 1), new Vector3(halfPi, -halfPi, halfPi)), 
                new Vertex(new Vector3(width, -height, -length), new Vector2(0, 0), new Vector3(halfPi, -halfPi, -halfPi)), 
                new Vertex(new Vector3(-width, -height, -length), new Vector2(1, 0), new Vector3(-halfPi, -halfPi, -halfPi)), 
            };

            var indices = new uint[]
            {
                // Top
                0, 1, 2,
                2, 3, 0,

                // Bottom
                4, 7, 6,
                6, 5, 4,

                // Left
                0, 3, 7,
                7, 4, 0,

                // Front
                3, 2, 6,
                6, 7, 3,

                // Right
                2, 1, 5,
                5, 6, 2,

                // Back
                1, 0, 4,
                4, 5, 1
            };

            _material = resourceCollection.CreateMaterial(ObjectBus);
            _mesh = resourceCollection.CreateMesh(vertices, indices);
        }

        public void Dispose()
        {
            _material.Dispose();
            _mesh?.Dispose();
        }
    }
}
