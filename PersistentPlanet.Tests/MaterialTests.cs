using System;
using MemBus;
using Moq;
using PersistentPlanet.Graphics;
using PersistentPlanet.Graphics.DirectX11;
using Xunit;

namespace PersistentPlanet.Tests
{
    public class MaterialTests
    {
        public class MaterialInitialiseTests
        {
            private readonly Mock<IShader> _pixelShader;
            private readonly Mock<IShader> _vertexShader;
            private readonly D11InitialiseContext _initialiseContext;
            private IBus _objectBus;

            public MaterialInitialiseTests()
            {
                var pixelShaderFactory = new Mock<Func<string, string, IShader>>();
                _pixelShader = new Mock<IShader>();
                pixelShaderFactory.SetReturnsDefault(_pixelShader.Object);

                var vertexShaderFactory = new Mock<Func<string, string, IShader>>();
                _vertexShader = new Mock<IShader>();
                vertexShaderFactory.SetReturnsDefault(_vertexShader.Object);

                var material = new Material(pixelShaderFactory.Object, vertexShaderFactory.Object);
                _initialiseContext = new D11InitialiseContext();
                _objectBus = Mock.Of<IBus>();
                material.Initialise(_initialiseContext);
            }

            [Fact]
            public void ThenThePixelShaderIsInitialised()
            {
                _pixelShader.Verify(ps => ps.Initialise(_initialiseContext));
            }

            [Fact]
            public void ThenTheVertexShaderIsInitialised()
            {
                _vertexShader.Verify(ps => ps.Initialise(_initialiseContext));
            }
        }

        public class MaterialDisposeTests
        {
            private readonly Mock<IShader> _pixelShader;
            private readonly Mock<IShader> _vertexShader;

            public MaterialDisposeTests()
            {
                var pixelShaderFactory = new Mock<Func<string, string, IShader>>();
                _pixelShader = new Mock<IShader>();
                pixelShaderFactory.SetReturnsDefault(_pixelShader.Object);

                var vertexShaderFactory = new Mock<Func<string, string, IShader>>();
                _vertexShader = new Mock<IShader>();
                vertexShaderFactory.SetReturnsDefault(_vertexShader.Object);

                var material = new Material(pixelShaderFactory.Object, vertexShaderFactory.Object);
                material.Initialise(new D11InitialiseContext());
                material.Dispose();
            }

            [Fact]
            public void ThenThePixelShaderIsDisposed()
            {
                _pixelShader.Verify(ps => ps.Dispose());
            }

            [Fact]
            public void ThenTheVertexShaderIsDisposed()
            {
                _vertexShader.Verify(vs => vs.Dispose());
            }
        }

        public class MaterialRenderTests
        {
            private readonly Mock<IShader> _pixelShader;
            private readonly Mock<IShader> _vertexShader;
            private readonly D11RenderContext _renderContext;

            public MaterialRenderTests()
            {
                var pixelShaderFactory = new Mock<Func<string, string, IShader>>();
                _pixelShader = new Mock<IShader>();
                pixelShaderFactory.SetReturnsDefault(_pixelShader.Object);

                var vertexShaderFactory = new Mock<Func<string, string, IShader>>();
                _vertexShader = new Mock<IShader>();
                vertexShaderFactory.SetReturnsDefault(_vertexShader.Object);

                var material = new Material(pixelShaderFactory.Object, vertexShaderFactory.Object);
                material.Initialise(new D11InitialiseContext());
                _renderContext = new D11RenderContext();
                material.Render(_renderContext);
            }

            [Fact]
            public void ThenThePixelShaderIsApplied()
            {
                _pixelShader.Verify(ps => ps.Apply(_renderContext));
            }

            [Fact]
            public void ThenTheVertexShaderIsApplied()
            {
                _vertexShader.Verify(vs => vs.Apply(_renderContext));
            }
        }
    }
}
