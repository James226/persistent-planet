﻿using MemBus;
using PersistentPlanet.Controls;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Vector4 = System.Numerics.Vector4;

namespace PersistentPlanet
{
    public class VertexShader : IShader
    {
        private readonly IBus _objectBus;
        private readonly string _filename;
        private readonly string _function;
        private SharpDX.Direct3D11.VertexShader _vertexShader;
        private byte[] _inputSignature;
        private InputLayout _inputLayout;
        private Buffer _lightBuffer;
        private Buffer _objectVsBuffer;
        private Matrix _worldMatrix = Matrix.Identity;

        public VertexShader(IBus objectBus, string filename, string function)
        {
            _objectBus = objectBus;
            _filename = filename;
            _function = function;
        }

        public void Initialise(IInitialiseContext context)
        {
            _objectBus.Subscribe<WorldMatrixUpdatedEvent>(e => _worldMatrix = e.WorldMatrix);
            
            using (var vertexShaderByteCode =
                ShaderBytecode.CompileFromFile(_filename, _function, "vs_4_0", ShaderFlags.Debug))
            {
                _vertexShader = new SharpDX.Direct3D11.VertexShader(context.Device, vertexShaderByteCode);

                InputElement[] inputElements =
                {
                    new InputElement("POSITION", 0, Format.R32G32B32_Float, 0),
                    new InputElement("TEXCOORD", 0, Format.R32G32_Float, 0),
                    new InputElement("NORMAL", 0, Format.R32G32B32_Float, 0)
                };
                _inputSignature = ShaderSignature.GetInputSignature(vertexShaderByteCode);
                _inputLayout = new InputLayout(context.Device, _inputSignature, inputElements);

                _lightBuffer = new Buffer(context.Device,
                                          Utilities.SizeOf<LightBufferType>(),
                                          ResourceUsage.Default,
                                          BindFlags.ConstantBuffer,
                                          CpuAccessFlags.None,
                                          ResourceOptionFlags.None,
                                          0);

                _objectVsBuffer = new Buffer(context.Device,
                                             Utilities.SizeOf<Matrix>(),
                                             ResourceUsage.Default,
                                             BindFlags.ConstantBuffer,
                                             CpuAccessFlags.None,
                                             ResourceOptionFlags.None,
                                             0);
            }
        }

        public void Dispose()
        {
            _vertexShader?.Dispose();
            _inputLayout?.Dispose();
            _lightBuffer?.Dispose();
            _objectVsBuffer?.Dispose();
        }

        public void Apply(IRenderContext renderContext)
        {
            var light = new LightBufferType
            {
                ambientColor = new Vector4(0.05f, 0.05f, 0.05f, 1.0f),
                diffuseColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
                lightDirection = new Vector3(0.2f, -0.2f, 0.2f)
            };
            renderContext.Context.UpdateSubresource(ref light, _lightBuffer);
            renderContext.Context.UpdateSubresource(ref _worldMatrix, _objectVsBuffer);

            renderContext.Context.VertexShader.Set(_vertexShader);
            renderContext.Context.VertexShader.SetConstantBuffer(1, _objectVsBuffer);
            renderContext.Context.PixelShader.SetConstantBuffer(0, _lightBuffer);

            renderContext.Context.InputAssembler.InputLayout = _inputLayout;
        }
    }
}