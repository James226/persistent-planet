using System;
using System.Numerics;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

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
        private Buffer _triangleVertexBuffer;
        private Vector3[] _vertices;

        public void Initialise(Device device)
        {
            _vertices = new[]
            {
                new Vector3(-0.5f, 0.5f, 0.0f),
                new Vector3(0.5f, 0.5f, 0.0f),
                new Vector3(0.0f, -0.5f, 0.0f)
            };
            _triangleVertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, _vertices);


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

            _triangleVertexBuffer.Dispose();
        }

        public void Render(DeviceContext deviceContext)
        {
            deviceContext.VertexShader.Set(_vertexShader);
            deviceContext.PixelShader.Set(_pixelShader);

            deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            deviceContext.InputAssembler.InputLayout = _inputLayout;
            deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_triangleVertexBuffer, Utilities.SizeOf<Vector3>(), 0));

            deviceContext.Draw(_vertices.Length, 0);
        }
    }
}