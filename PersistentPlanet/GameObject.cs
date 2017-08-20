using System;
using System.Drawing;
using System.Linq;
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
        public readonly Vector2 Texture;
        public readonly Vector3 Normal;

        public Vertex(Vector3 position, Vector2 texture, Vector3 normal)
        {
            Position = position;
            Texture = texture;
            Normal = normal;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LightBufferType
    {
        public Vector4 ambientColor;
        public Vector4 diffuseColor;
        public Vector3 lightDirection;
        public float padding;
    };

    public struct HeightMapType
    {
        public float x, y, z;
        public float tu, tv;
        public float nx, ny, nz;
    };


    public class GameObject : IDisposable
    {
        private const int TextureRepeat = 32;

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
        private HeightMapType[] _heightmap;
        private Buffer _objectVsBuffer;
        private ShaderResourceView _texture;

        public void Initialise(Device device, DeviceContext deviceContext)
        {
            GenerateBuffers(device);

            _vertexShaderByteCode =
                ShaderBytecode.CompileFromFile("vertexShader.hlsl", "main", "vs_4_0", ShaderFlags.Debug);
            _pixelShaderByteCode =
                ShaderBytecode.CompileFromFile("pixelShader.hlsl", "main", "ps_4_0", ShaderFlags.Debug);

            _vertexShader = new VertexShader(device, _vertexShaderByteCode);
            _pixelShader = new PixelShader(device, _pixelShaderByteCode);

            InputElement[] inputElements =
            {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, 0),
                new InputElement("NORMAL", 0, Format.R32G32B32_Float, 0)
            };
            _inputSignature = ShaderSignature.GetInputSignature(_vertexShaderByteCode);
            _inputLayout = new InputLayout(device, _inputSignature, inputElements);

            _lightBuffer = new Buffer(device,
                                      Utilities.SizeOf<LightBufferType>(),
                                      ResourceUsage.Default,
                                      BindFlags.ConstantBuffer,
                                      CpuAccessFlags.None,
                                      ResourceOptionFlags.None,
                                      0);

            _objectVsBuffer = new Buffer(device,
                                         Utilities.SizeOf<Matrix>(),
                                         ResourceUsage.Default,
                                         BindFlags.ConstantBuffer,
                                         CpuAccessFlags.None,
                                         ResourceOptionFlags.None,
                                         0);

            var worldMatrix = Matrix.Identity;

            deviceContext.UpdateSubresource(ref worldMatrix, _objectVsBuffer);

            var light = new LightBufferType
            {
                ambientColor = new Vector4(0.05f, 0.05f, 0.05f, 1.0f),
                diffuseColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
                lightDirection = new Vector3(0.2f, -0.2f, 0.2f)
            };

            deviceContext.UpdateSubresource(ref light, _lightBuffer);
        }

        private void GenerateBuffers(Device device)
        {
            var image = new Bitmap("heightmap.bmp");
            var terrainWidth = image.Width;
            var terrainHeight = image.Height;

            using (var bitmap = TextureLoader.LoadBitmap(new SharpDX.WIC.ImagingFactory2(), "sand.jpg"))
            using (var texture = TextureLoader.CreateTexture2DFromBitmap(device, bitmap))
            {
                _texture = new ShaderResourceView(device, texture);
            }

            byte GetHeight(int x, int z)
            {
                var height = image.GetPixel(x, z).R;

                return (byte) ((height / 256f) * 64);
            }

            _heightmap = new HeightMapType[terrainWidth * terrainHeight];

            for (var j = 0; j < terrainHeight; j++)
            {
                for (var i = 0; i < terrainWidth; i++)
                {
                    var height = GetHeight(i, j);

                    var idx = (terrainWidth * j) + i;

                    _heightmap[idx].x = i;
                    _heightmap[idx].y = height;
                    _heightmap[idx].z = j;
                }
            }

            CalculateNormals(terrainWidth, terrainHeight);
            CalculateTextureCoordinates(terrainWidth, terrainHeight);

            var vertexCount = (terrainWidth - 1) * (terrainHeight - 1) * 8;
            var indexCount = vertexCount;

            var vertices = new Vertex[vertexCount];
            var indices = new uint[indexCount];

            uint index = 0;

            for (var j = 0; j < terrainHeight - 1; j++)
            for (var i = 0; i < terrainWidth - 1; i++)
            {

                var index1 = (terrainWidth * j) + i; // Bottom left.
                var index2 = (terrainWidth * j) + (i + 1); // Bottom right.
                var index3 = (terrainWidth * (j + 1)) + i; // Upper left.
                var index4 = (terrainWidth * (j + 1)) + (i + 1); // Upper right.

                void AddIndex(int idx)
                {
                    vertices[index] = new Vertex(new Vector3(_heightmap[idx].x, _heightmap[idx].y, _heightmap[idx].z),
                                                 new Vector2(_heightmap[idx].tu, _heightmap[idx].tv),
                                                 new Vector3(_heightmap[idx].nx,
                                                             _heightmap[idx].ny,
                                                             _heightmap[idx].nz));
                    indices[index] = index;
                    index++;
                }

                AddIndex(index3); // Upper left.
                AddIndex(index4); // Upper right.
                AddIndex(index1); // Bottom left.
                AddIndex(index1); // Bottom left.
                AddIndex(index4); // Upper right.
                AddIndex(index2); // Bottom right.
            }


            image.Dispose();

            _vertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, vertices);

            _indices = indices.ToArray();

            _indexBuffer = Buffer.Create(device, BindFlags.IndexBuffer, _indices);
        }

        private void CalculateNormals(int terrainWidth, int terrainHeight)
        {
            int i, j;
            int index;

            // Create a temporary array to hold the un-normalized normal vectors.
            var normals = new Vector3[(terrainHeight - 1) * (terrainWidth - 1)];

            // Go through all the faces in the mesh and calculate their normals.
            for (j = 0; j < (terrainHeight - 1); j++)
            {
                for (i = 0; i < (terrainWidth - 1); i++)
                {
                    var index1 = (j * terrainWidth) + i;
                    var index2 = (j * terrainWidth) + (i + 1);
                    var index3 = ((j + 1) * terrainWidth) + i;

                    // Get three vertices from the face.
                    var vertex1 = new Vector3(_heightmap[index1].x, _heightmap[index1].y, _heightmap[index2].z);
                    var vertex2 = new Vector3(_heightmap[index2].x, _heightmap[index2].y, _heightmap[index2].z);
                    var vertex3 = new Vector3(_heightmap[index3].x, _heightmap[index3].y, _heightmap[index3].z);


                    // Calculate the two vectors for this face.
                    var vector1 = vertex1 - vertex3;
                    var vector2 = vertex3 - vertex2;

                    index = (j * (terrainWidth - 1)) + i;

                    // Calculate the cross product of those two vectors to get the un-normalized value for this face normal.
                    normals[index] = Vector3.Cross(vector1, vector2);
                }
            }

            // Now go through all the vertices and take an average of each face normal 	
            // that the vertex touches to get the averaged normal for that vertex.
            for (j = 0; j < terrainHeight; j++)
            {
                for (i = 0; i < terrainWidth; i++)
                {
                    // Initialize the sum.
                    var sum = new Vector3();

                    // Initialize the count.
                    var count = 0;

                    // Bottom left face.
                    if (((i - 1) >= 0) && ((j - 1) >= 0))
                    {
                        index = ((j - 1) * (terrainWidth - 1)) + (i - 1);

                        sum += normals[index];
                        count++;
                    }

                    // Bottom right face.
                    if ((i < (terrainWidth - 1)) && ((j - 1) >= 0))
                    {
                        index = ((j - 1) * (terrainWidth - 1)) + i;

                        sum += normals[index];
                        count++;
                    }

                    // Upper left face.
                    if (((i - 1) >= 0) && (j < (terrainHeight - 1)))
                    {
                        index = (j * (terrainWidth - 1)) + (i - 1);

                        sum += normals[index];
                        count++;
                    }

                    // Upper right face.
                    if ((i < (terrainWidth - 1)) && (j < (terrainHeight - 1)))
                    {
                        index = (j * (terrainWidth - 1)) + i;

                        sum += normals[index];
                        count++;
                    }

                    // Take the average of the faces touching this vertex.
                    sum /= (float) count;

                    // Calculate the length of this normal.
                    var length = sum.Length();

                    // Get an index to the vertex location in the height map array.
                    index = (j * terrainWidth) + i;

                    // Normalize the final shared normal for this vertex and store it in the height map array.
                    _heightmap[index].nx = (sum.X / length);
                    _heightmap[index].ny = (sum.Y / length);
                    _heightmap[index].nz = (sum.Z / length);
                }
            }
        }

        private void CalculateTextureCoordinates(int terrainWidth, int terrainHeight)
        {
            int incrementCount;
            int tuCount, tvCount;
            float incrementValue, tuCoordinate, tvCoordinate;


            // Calculate how much to increment the texture coordinates by.
            incrementValue = TextureRepeat / (float) terrainWidth;

            // Calculate how many times to repeat the texture.
            incrementCount = terrainWidth / TextureRepeat;

            // Initialize the tu and tv coordinate values.
            tuCoordinate = 0.0f;
            tvCoordinate = 1.0f;

            // Initialize the tu and tv coordinate indexes.
            tuCount = 0;
            tvCount = 0;

            // Loop through the entire height map and calculate the tu and tv texture coordinates for each vertex.
            for (var j = 0; j < terrainHeight; j++)
            {
                for (var i = 0; i < terrainWidth; i++)
                {
                    // Store the texture coordinate in the height map.
                    _heightmap[(terrainWidth * j) + i].tu = tuCoordinate;
                    _heightmap[(terrainWidth * j) + i].tv = tvCoordinate;

                    // Increment the tu texture coordinate by the increment value and increment the index by one.
                    tuCoordinate += incrementValue;
                    tuCount++;

                    // Check if at the far right end of the texture and if so then start at the beginning again.
                    if (tuCount == incrementCount)
                    {
                        tuCoordinate = 0.0f;
                        tuCount = 0;
                    }
                }

                // Increment the tv texture coordinate by the increment value and increment the index by one.
                tvCoordinate -= incrementValue;
                tvCount++;

                // Check if at the top of the texture and if so then start at the bottom again.
                if (tvCount == incrementCount)
                {
                    tvCoordinate = 1.0f;
                    tvCount = 0;
                }
            }
        }

        public void Dispose()
        {
            _texture.Dispose();

            _lightBuffer.Dispose();
            _inputLayout.Dispose();
            _inputSignature.Dispose();

            _objectVsBuffer.Dispose();

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
            deviceContext.VertexShader.SetConstantBuffer(1, _objectVsBuffer);
            deviceContext.PixelShader.SetConstantBuffer(0, _lightBuffer);
            deviceContext.PixelShader.SetShaderResource(0, _texture);

            deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            deviceContext.InputAssembler.InputLayout = _inputLayout;
            deviceContext.InputAssembler.SetVertexBuffers(0,
                                                          new VertexBufferBinding(
                                                              _vertexBuffer,
                                                              Utilities.SizeOf<Vertex>(),
                                                              0));
            deviceContext.InputAssembler.SetIndexBuffer(_indexBuffer, Format.R32_UInt, 0);

            deviceContext.DrawIndexed(_indices.Length, 0, 0);
        }
    }
}