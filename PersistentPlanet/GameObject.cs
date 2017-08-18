using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Vector3 = System.Numerics.Vector3;

namespace PersistentPlanet
{
    public class GameObject : IDisposable
    {
        private CompilationResult _vertexShaderByteCode;
        private CompilationResult _pixelShaderByteCode;
        private VertexShader _vertexShader;
        private PixelShader _pixelShader;
        private ShaderSignature _inputSignature;
        private InputLayout _inputLayout;
        private Buffer _vertexBuffer;
        private Vector3[] _vertices;
        private uint[] _indices;
        private Buffer _indexBuffer;

        public void Initialise(Device device)
        {
            var vertices = new Vector3[50 * 50];
            var indices = new List<uint>();

            const uint rows = 50;
            const uint columns = 50;

            for (uint z = 0; z < rows; z++)
            {
                for (uint x = 0; x < columns; x++)
                {
                    var index = x + z * columns;
                    vertices[index] = new Vector3(x,0,z);
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

            _vertices = vertices.ToArray();

            _vertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, _vertices);


            _indices = indices.ToArray();

            _indexBuffer = Buffer.Create(device, BindFlags.IndexBuffer, _indices);

            _vertexShaderByteCode = ShaderBytecode.CompileFromFile("vertexShader.hlsl", "main", "vs_4_0", ShaderFlags.Debug);
            _pixelShaderByteCode = ShaderBytecode.CompileFromFile("pixelShader.hlsl", "main", "ps_4_0", ShaderFlags.Debug);

            _vertexShader = new VertexShader(device, _vertexShaderByteCode);
            _pixelShader = new PixelShader(device, _pixelShaderByteCode);

            InputElement[] inputElements =
            {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0)
            };
            _inputSignature = ShaderSignature.GetInputSignature(_vertexShaderByteCode);
            _inputLayout = new InputLayout(device, _inputSignature, inputElements);
        }

        public void Dispose()
        {
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

            deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            deviceContext.InputAssembler.InputLayout = _inputLayout;
            deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vertexBuffer, Utilities.SizeOf<Vector3>(), 0));
            deviceContext.InputAssembler.SetIndexBuffer(_indexBuffer, Format.R32_UInt, 0);

            deviceContext.DrawIndexed(_indices.Length, 0, 0);
        }
    }
}