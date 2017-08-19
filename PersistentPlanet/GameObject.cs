using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Numerics;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace PersistentPlanet
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public readonly Vector3 Position;
        public readonly Vector3 Normal;

        public Vertex(Vector3 position, Vector3 normal)
        {
            Position = position;
            Normal = normal;
        }
    }

    struct LightBufferType
    {
        public Vector4 ambientColor;
        public Vector4 diffuseColor;
        public Vector3 lightDirection;
        public float padding;
    };

    public class GameObject : IDisposable
    {
        private CompilationResult _vertexShaderByteCode;
        private CompilationResult _pixelShaderByteCode;
        private VertexShader _vertexShader;
        private PixelShader _pixelShader;
        private ShaderSignature _inputSignature;
        private InputLayout _inputLayout;
        private Buffer _vertexBuffer;
        private uint[] _indices;
        private Buffer _indexBuffer;
        private Buffer _lightBuffer;

        public void Initialise(Device device, DeviceContext deviceContext)
        {
            GenerateBuffers2(device);
            
            _vertexShaderByteCode = ShaderBytecode.CompileFromFile("vertexShader.hlsl", "main", "vs_4_0", ShaderFlags.Debug);
            _pixelShaderByteCode = ShaderBytecode.CompileFromFile("pixelShader.hlsl", "main", "ps_4_0", ShaderFlags.Debug);

            _vertexShader = new VertexShader(device, _vertexShaderByteCode);
            _pixelShader = new PixelShader(device, _pixelShaderByteCode);

            InputElement[] inputElements =
            {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0),
                new InputElement("NORMAL", 0, Format.R32G32B32_Float, 0)
            };
            _inputSignature = ShaderSignature.GetInputSignature(_vertexShaderByteCode);
            _inputLayout = new InputLayout(device, _inputSignature, inputElements);

            _lightBuffer = new Buffer(device,
                                      Utilities.SizeOf<LightBufferType>(),
                                      ResourceUsage.Dynamic,
                                      BindFlags.ConstantBuffer,
                                      CpuAccessFlags.Write,
                                      ResourceOptionFlags.None,
                                      0);

            var light = new LightBufferType
            {
                ambientColor = new Vector4(0.05f, 0.05f, 0.05f, 1.0f),
                diffuseColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
                lightDirection = new Vector3(0.0f, 0.0f, 0.75f)
            };

            deviceContext.UpdateSubresource(ref light, _lightBuffer);

        }

        private void GenerateBuffers(Device device)
        {
            var vertices = new Vertex[50 * 50];
            var indices = new List<uint>();

            const uint rows = 50;
            const uint columns = 50;

            for (uint z = 0; z < rows; z++)
            {
                for (uint x = 0; x < columns; x++)
                {
                    var index = x + z * columns;
                    vertices[index] = new Vertex(new Vector3(x, 0, z), new Vector3(0, -1, 0));
                }
            }

            for (uint z = 0; z < rows - 1; z++)
            {
                if (z != 0) indices.Add(z * columns);

                for (uint x = 0; x < columns; x++)
                {
                    indices.Add(z * columns + x);
                    indices.Add((z + 1) * columns + x);
                }

                if (z != columns - 2) indices.Add((z + 1) * columns + (rows - 1));
            }

            _vertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, vertices);


            _indices = indices.ToArray();

            _indexBuffer = Buffer.Create(device, BindFlags.IndexBuffer, _indices);
        }

        private void GenerateBuffers2(Device device)
        {
            var image = new Bitmap("heightmap.bmp");

            var terrainWidth = image.Width;
            var terrainHeight = image.Height;

            var vertexCount = (terrainWidth - 1) * (terrainHeight - 1) * 8;
            var indexCount = vertexCount;

            var vertices = new Vertex[vertexCount];
            var indices = new uint[indexCount];

            uint index = 0;


            byte GetHeight(int x, int z)
            {
                byte height = (byte)(255 - image.GetPixel(x, z).R);

                return (byte)((height / 256f) * 50);
            }

            for (var j = 0; j < terrainHeight - 1; j++)
            for (var i = 0; i < terrainWidth - 1; i++)
            {



                int positionX = i;
                int positionZ = j + 1;
                vertices[index] = new Vertex(new Vector3(positionX, GetHeight(positionX, positionZ), positionZ),
                                             new Vector3(0, 1, 0));
                indices[index] = index;

                index++;

                // Upper right.
                positionX = i + 1;
                positionZ = j + 1;

                vertices[index] = new Vertex(new Vector3(positionX, GetHeight(positionX, positionZ), positionZ),
                                             new Vector3(0, 1, 0));
                indices[index] = index;
                index++;

                // LINE 2
                // Upper right.
                positionX = i + 1;
                positionZ = j + 1;

                vertices[index] = new Vertex(new Vector3(positionX, GetHeight(positionX, positionZ), positionZ),
                                             new Vector3(0, 1, 0));
                indices[index] = index;
                index++;

                // Bottom right.
                positionX = i + 1;
                positionZ = j;

                vertices[index] = new Vertex(new Vector3(positionX, GetHeight(positionX, positionZ), positionZ),
                                             new Vector3(0, 1, 0));
                indices[index] = index;
                index++;

                // LINE 3
                // Bottom right.
                positionX = i + 1;
                positionZ = j;

                vertices[index] = new Vertex(new Vector3(positionX, GetHeight(positionX, positionZ), positionZ),
                                             new Vector3(0, 1, 0));
                indices[index] = index;
                index++;

                // Bottom left.
                positionX = i;
                positionZ = j;

                vertices[index] = new Vertex(new Vector3(positionX, GetHeight(positionX, positionZ), positionZ),
                                             new Vector3(0, 1, 0));
                indices[index] = index;
                index++;

                // LINE 4
                // Bottom left.
                positionX = i;
                positionZ = j;

                vertices[index] = new Vertex(new Vector3(positionX, GetHeight(positionX, positionZ), positionZ),
                                             new Vector3(0, 1, 0));
                indices[index] = index;
                index++;

                // Upper left.
                positionX = i;
                positionZ = j + 1;

                vertices[index] = new Vertex(new Vector3(positionX, GetHeight(positionX, positionZ), positionZ),
                                             new Vector3(0, 1, 0));
                indices[index] = index;
                index++;

            }

            image.Dispose();

            _vertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, vertices);

            _indices = indices.ToArray();

            _indexBuffer = Buffer.Create(device, BindFlags.IndexBuffer, _indices);
        }

        public void Dispose()
        {
            _lightBuffer.Dispose();
            _inputLayout.Dispose();
            _inputSignature.Dispose();

            _pixelShader.Dispose();
            _vertexShader.Dispose();

            _pixelShaderByteCode.Dispose();
            _vertexShaderByteCode.Dispose();

            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
        }

        public void Render(DeviceContext deviceContext)
        {
            deviceContext.VertexShader.Set(_vertexShader);
            deviceContext.PixelShader.Set(_pixelShader);
            deviceContext.PixelShader.SetConstantBuffer(0, _lightBuffer);

            deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
            deviceContext.InputAssembler.InputLayout = _inputLayout;
            deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertexBuffer, Utilities.SizeOf<Vertex>(), 0));
            deviceContext.InputAssembler.SetIndexBuffer(_indexBuffer, Format.R32_UInt, 0);

            deviceContext.DrawIndexed(_indices.Length, 0, 0);
        }
    }
}