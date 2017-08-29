using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using MemBus;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;
using Vector2 = SharpDX.Vector2;
using Vector3 = SharpDX.Vector3;

namespace PersistentPlanet.Primitives
{
    public class Cube : IComponent
    {
        public IBus ObjectBus { get; set; }

        private Buffer _vertexBuffer;
        private Buffer _indexBuffer;
        private Material _material;
        private int _bufferSize;

        public void Initialise(InitialiseContext context)
        {
            _material = new Material(ObjectBus);
            _material.Initialise(context);

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

            var indices = new []
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

            _bufferSize = indices.Length;

            _vertexBuffer = Buffer.Create(context.Device, BindFlags.VertexBuffer, vertices);
            _indexBuffer = Buffer.Create(context.Device, BindFlags.IndexBuffer, indices);
        }

        public void Dispose()
        {
            _indexBuffer.Dispose();
            _vertexBuffer.Dispose();
            _material.Dispose();
        }

        public void Render(IRenderContext context)
        {
            _material.Render(context);
            context.Context.InputAssembler.SetVertexBuffers(0,
                                                            new VertexBufferBinding(
                                                                _vertexBuffer,
                                                                Utilities.SizeOf<Vertex>(),
                                                                0));

            context.Context.InputAssembler.SetIndexBuffer(_indexBuffer, Format.R32_UInt, 0);

            context.Context.DrawIndexed(_bufferSize, 0, 0);
        }
    }
}
