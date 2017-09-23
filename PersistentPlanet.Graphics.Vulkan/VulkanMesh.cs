using System;
using System.Numerics;
using System.Runtime.InteropServices;
using VulkanCore;

namespace PersistentPlanet.Graphics.Vulkan
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ObjectParams
    {
        public Matrix4x4 World;
    }

    public class VulkanMesh : IMesh<VulkanRenderContext>
    {
        private VulkanBuffer _vertexBuffer;
        private VulkanBuffer _indexBuffer;
        private PipelineLayout _pipelineLayout;
        private Pipeline _pipeline;
        private DescriptorSetLayout _descriptorSetLayout;
        private DescriptorPool _descriptorPool;
        private DescriptorSet _descriptorSet;
        private VulkanBuffer _objectBuffer;
        private VulkanImage _cubeTexture;
        private Sampler _sampler;

        public void Initialise(VulkanInitialiseContext context, Vertex[] vertices, uint[] indices)
        {
            _cubeTexture = context.Content.Load<VulkanImage>("sand.ktx");
            _vertexBuffer = VulkanBuffer.Vertex(context.Context, vertices);
            _indexBuffer = VulkanBuffer.Index(context.Context, indices);

            var descriptorPoolSizes = new[]
            {
                new DescriptorPoolSize(DescriptorType.UniformBuffer, 1),
                new DescriptorPoolSize(DescriptorType.UniformBuffer, 1),
                new DescriptorPoolSize(DescriptorType.CombinedImageSampler, 1)
            };
            _descriptorPool = context.Context.Device.CreateDescriptorPool(
                new DescriptorPoolCreateInfo(descriptorPoolSizes.Length, descriptorPoolSizes));

            _objectBuffer = VulkanBuffer.DynamicUniform<ObjectParams>(context.Context, 1);

            var objectParams = new ObjectParams()
            {
                World = Matrix4x4.CreateTranslation(50, 0, 0), //Matrix4x4.CreateFromYawPitchRoll(0, MathF.PI / 4, MathF.PI / 4),
            };

            var ptr = _objectBuffer.Memory.Map(0, Interop.SizeOf<WorldBuffer>());
            Interop.Write(ptr, ref objectParams);
            _objectBuffer.Memory.Unmap();

            _descriptorSetLayout = context.Context.Device.CreateDescriptorSetLayout(new DescriptorSetLayoutCreateInfo(
                                                                                        new DescriptorSetLayoutBinding(0, DescriptorType.UniformBuffer, 1, ShaderStages.Vertex),
                                                                                        new DescriptorSetLayoutBinding(1, DescriptorType.UniformBuffer, 1, ShaderStages.Vertex),
                                                                                        new DescriptorSetLayoutBinding(2, DescriptorType.CombinedImageSampler, 1, ShaderStages.Fragment)));

            _pipelineLayout = context.Context.Device.CreatePipelineLayout(new PipelineLayoutCreateInfo(
                                                                              new[] { _descriptorSetLayout }));

            _descriptorSet = _descriptorPool.AllocateSets(new DescriptorSetAllocateInfo(1, _descriptorSetLayout))[0];

            var createInfo = new SamplerCreateInfo
            {
                MagFilter = Filter.Linear,
                MinFilter = Filter.Linear,
                MipmapMode = SamplerMipmapMode.Linear
            };
            // We also enable anisotropic filtering. Because that feature is optional, it must be
            // checked if it is supported by the device.
            if (context.Context.Features.SamplerAnisotropy)
            {
                createInfo.AnisotropyEnable = true;
                createInfo.MaxAnisotropy = context.Context.Properties.Limits.MaxSamplerAnisotropy;
            }
            else
            {
                createInfo.MaxAnisotropy = 1.0f;
            }
            _sampler = context.Context.Device.CreateSampler(createInfo);

            // Update the descriptor set for the shader binding point.
            var writeDescriptorSets = new[]
            {
                new WriteDescriptorSet(_descriptorSet, 0, 0, 1, DescriptorType.UniformBuffer,
                                       bufferInfo: new[] { new DescriptorBufferInfo(context.WorldBuffer) }),
                new WriteDescriptorSet(_descriptorSet, 1, 0, 1, DescriptorType.UniformBuffer,
                                       bufferInfo: new[] { new DescriptorBufferInfo(_objectBuffer) }),
                new WriteDescriptorSet(_descriptorSet, 2, 0, 1, DescriptorType.CombinedImageSampler,
                                       imageInfo: new[] { new DescriptorImageInfo(_sampler, _cubeTexture.View, ImageLayout.General) })
            };
            _descriptorPool.UpdateSets(writeDescriptorSets);

            ShaderModule vertexShader = context.Content.Load<ShaderModule>("Shader.vert.spv");
            ShaderModule fragmentShader = context.Content.Load<ShaderModule>("Shader.frag.spv");
            var shaderStageCreateInfos = new[]
            {
                new PipelineShaderStageCreateInfo(ShaderStages.Vertex, vertexShader, "main"),
                new PipelineShaderStageCreateInfo(ShaderStages.Fragment, fragmentShader, "main")
            };

            var vertexInputStateCreateInfo = new PipelineVertexInputStateCreateInfo(
                new[] { new VertexInputBindingDescription(0, Interop.SizeOf<Vertex>(), VertexInputRate.Vertex) },
                new[]
                {
                    new VertexInputAttributeDescription(0, 0, Format.R32G32B32SFloat, 0),  // Position.
                    new VertexInputAttributeDescription(1, 0, Format.R32G32B32SFloat, 12), // Normal.
                    new VertexInputAttributeDescription(2, 0, Format.R32G32SFloat, 24)     // TexCoord.
                }
            );
            var inputAssemblyStateCreateInfo = new PipelineInputAssemblyStateCreateInfo(PrimitiveTopology.TriangleList);
            var viewportStateCreateInfo = new PipelineViewportStateCreateInfo(
                new Viewport(0, 0, context.RenderWindow.WindowWidth, context.RenderWindow.WindowHeight),
                new Rect2D(0, 0, context.RenderWindow.WindowWidth, context.RenderWindow.WindowHeight));
            var rasterizationStateCreateInfo = new PipelineRasterizationStateCreateInfo
            {
                PolygonMode = PolygonMode.Fill,
                CullMode = CullModes.Front,
                FrontFace = FrontFace.CounterClockwise,
                LineWidth = 1.0f
            };
            var multisampleStateCreateInfo = new PipelineMultisampleStateCreateInfo
            {
                RasterizationSamples = SampleCounts.Count1,
                MinSampleShading = 1.0f
            };
            var depthStencilCreateInfo = new PipelineDepthStencilStateCreateInfo
            {
                DepthTestEnable = true,
                DepthWriteEnable = true,
                DepthCompareOp = CompareOp.GreaterOrEqual,
                Back = new StencilOpState
                {
                    FailOp = StencilOp.Keep,
                    PassOp = StencilOp.Keep,
                    CompareOp = CompareOp.Always
                },
                Front = new StencilOpState
                {
                    FailOp = StencilOp.Keep,
                    PassOp = StencilOp.Keep,
                    CompareOp = CompareOp.Always
                }
            };
            var colorBlendAttachmentState = new PipelineColorBlendAttachmentState
            {
                SrcColorBlendFactor = BlendFactor.One,
                DstColorBlendFactor = BlendFactor.Zero,
                ColorBlendOp = BlendOp.Add,
                SrcAlphaBlendFactor = BlendFactor.One,
                DstAlphaBlendFactor = BlendFactor.Zero,
                AlphaBlendOp = BlendOp.Add,
                ColorWriteMask = ColorComponents.All
            };
            var colorBlendStateCreateInfo = new PipelineColorBlendStateCreateInfo(
                new[] { colorBlendAttachmentState });


            var pipelineCreateInfo = new GraphicsPipelineCreateInfo(
                _pipelineLayout, context.RenderPass, 0,
                shaderStageCreateInfos,
                inputAssemblyStateCreateInfo,
                vertexInputStateCreateInfo,
                rasterizationStateCreateInfo,
                viewportState: viewportStateCreateInfo,
                multisampleState: multisampleStateCreateInfo,
                depthStencilState: depthStencilCreateInfo,
                colorBlendState: colorBlendStateCreateInfo);
            _pipeline = context.Context.Device.CreateGraphicsPipeline(pipelineCreateInfo);
        }

        public void Dispose()
        {
        }

        public void Render(VulkanRenderContext context)
        {
            context.CommandBuffer.CmdBindPipeline(PipelineBindPoint.Graphics, _pipeline);
            context.CommandBuffer.CmdBindDescriptorSet(PipelineBindPoint.Graphics, _pipelineLayout, _descriptorSet);
            context.CommandBuffer.CmdBindVertexBuffer(_vertexBuffer);
            context.CommandBuffer.CmdBindIndexBuffer(_indexBuffer);
            context.CommandBuffer.CmdDrawIndexed(_indexBuffer.Count);
        }
    }
}
