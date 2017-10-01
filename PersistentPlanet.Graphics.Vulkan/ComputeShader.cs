using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using VulkanCore;

namespace PersistentPlanet.Graphics.Vulkan
{
    [StructLayout(LayoutKind.Sequential)]

    public struct ComputeData
    {
        public int Value;

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    public class ComputeShader : IDisposable
    {
        private CommandBuffer _commandBuffer;
        private DescriptorSetLayout _descriptorSetLayout;
        private PipelineLayout _pipelineLayout;
        private Pipeline _pipeline;
        private DescriptorPool _descriptorPool;
        private VulkanBuffer _storageBuffer;
        private DescriptorSet _descriptorSet;
        private VulkanContext _context;

        public void Initialise(VulkanInitialiseContext context)
        {
            _context = context.Context;
            var processFence = context.Context.Device.CreateFence();
            //_commandBuffer = context.Context.ComputeCommandPool.AllocateBuffers(new CommandBufferAllocateInfo(CommandBufferLevel.Primary, 1))[0];

            _descriptorPool = CreateDescriptorPool(context.Context.Device);
            _descriptorSetLayout = CreateComputeDescriptorSetLayout(context.Context.Device);
            _pipelineLayout = CreateComputePipelineLayout(context.Context.Device, _descriptorSetLayout);
            _pipeline = CreateComputePipeline(context.Context.Device, context.Content, _pipelineLayout);
            _storageBuffer = CreateStorageBuffer(context.Context);
            _descriptorSet = CreateComputeDescriptorSet(_descriptorPool, _descriptorSetLayout, _storageBuffer);
        }

        private DescriptorSetLayout CreateComputeDescriptorSetLayout(Device device)
        {
            return device.CreateDescriptorSetLayout(new DescriptorSetLayoutCreateInfo(
                                                                new DescriptorSetLayoutBinding(0, DescriptorType.StorageBuffer, 1, ShaderStages.Compute)
                                                                //new DescriptorSetLayoutBinding(1, DescriptorType.UniformBuffer, 1, ShaderStages.Compute)
                                                                ));
        }

        private DescriptorPool CreateDescriptorPool(Device device)
        {
            return device.CreateDescriptorPool(new DescriptorPoolCreateInfo(3, new[]
            {
                //new DescriptorPoolSize(DescriptorType.UniformBuffer, 1),
                new DescriptorPoolSize(DescriptorType.StorageBuffer, 1),
                //new DescriptorPoolSize(DescriptorType.CombinedImageSampler, 1)
            }));
        }

        private const int DataMultiplier = 2048;
        private const int Threads = 256;

        private VulkanBuffer CreateStorageBuffer(VulkanContext context)
        {
            var particles = new ComputeData[Threads * DataMultiplier];
            for (var i = 0; i < particles.Length; i++)
            {
                particles[i] = new ComputeData {Value = i};
            }

            return VulkanBuffer.Storage(context, particles);
        }

        private DescriptorSet CreateComputeDescriptorSet(DescriptorPool pool, DescriptorSetLayout layout, VulkanBuffer storageBuffer)
        {
            var descriptorSet = pool.AllocateSets(new DescriptorSetAllocateInfo(1, layout))[0];
            pool.UpdateSets(new[]
            {
                // Particles storage buffer.
                new WriteDescriptorSet(descriptorSet, 0, 0, 1, DescriptorType.StorageBuffer,
                                       bufferInfo: new[] { new DescriptorBufferInfo(storageBuffer) }),
                // Global simulation data (ie. delta time, etc).
                //new WriteDescriptorSet(descriptorSet, 1, 0, 1, DescriptorType.UniformBuffer,
                //                       bufferInfo: new[] { new DescriptorBufferInfo(_uniformBuffer) }),
            });
            return descriptorSet;
        }

        private PipelineLayout CreateComputePipelineLayout(Device device, DescriptorSetLayout descriptorSetLayout)
        {
            return device.CreatePipelineLayout(new PipelineLayoutCreateInfo(new[] { descriptorSetLayout }));
        }

        private Pipeline CreateComputePipeline(Device device, ContentManager content, PipelineLayout layout)
        {
            var pipelineCreateInfo = new ComputePipelineCreateInfo(
                new PipelineShaderStageCreateInfo(ShaderStages.Compute,
                                                  content.Load<ShaderModule>("shader.comp.spv"),
                                                  "main"),
                layout);
            return device.CreateComputePipeline(pipelineCreateInfo);
        }

        public void Dispose()
        {
            //_descriptorSet?.Dispose();
            _storageBuffer?.Dispose();
            _pipeline?.Dispose();
            _pipelineLayout?.Dispose();
            _descriptorSetLayout?.Dispose();
            _descriptorPool?.Dispose();
            _commandBuffer?.Dispose();
        }

        public void PrintResults(VulkanContext context)
        {
            var size = _storageBuffer.Count * Interop.SizeOf<ComputeData>();
            var stagingBuffer = context.Device.CreateBuffer(new BufferCreateInfo(size, BufferUsages.TransferDst));
            MemoryRequirements stagingReq = stagingBuffer.GetMemoryRequirements();
            int stagingMemoryTypeIndex = context.MemoryProperties.MemoryTypes.IndexOf(
                stagingReq.MemoryTypeBits,
                MemoryProperties.HostVisible | MemoryProperties.HostCoherent);
            DeviceMemory stagingMemory = context.Device.AllocateMemory(new MemoryAllocateInfo(stagingReq.Size, stagingMemoryTypeIndex));
            IntPtr vertexPtr = stagingMemory.Map(0, stagingReq.Size);
            Interop.Write(vertexPtr, Enumerable.Repeat(0, Threads * DataMultiplier).ToArray());
            stagingMemory.Unmap();
            stagingBuffer.BindMemory(stagingMemory);

            CommandBuffer cmdBuffer = context.GraphicsCommandPool.AllocateBuffers(new CommandBufferAllocateInfo(CommandBufferLevel.Primary, 1))[0];
            cmdBuffer.Begin(new CommandBufferBeginInfo(CommandBufferUsages.OneTimeSubmit));
            cmdBuffer.CmdCopyBuffer(_storageBuffer, stagingBuffer, new BufferCopy(size));
            cmdBuffer.End();

            // Submit.
            Fence fence = context.Device.CreateFence();
            context.GraphicsQueue.Submit(new SubmitInfo(commandBuffers: new[] { cmdBuffer }), fence);
            fence.Wait();

            // Cleanup.
            fence.Dispose();
            cmdBuffer.Dispose();

            var results = new ComputeData[Threads * DataMultiplier];
            var ptr = stagingMemory.Map(0, Interop.SizeOf<ComputeData>() * Threads * DataMultiplier);
            Interop.Read(ptr, results);
            stagingMemory.Unmap();


            stagingBuffer.Dispose();
            stagingMemory.Dispose();
        }

        public void Record(VulkanRenderContext context, CommandBuffer commandBuffer)
        {
            var graphicsToComputeBarrier = new BufferMemoryBarrier(_storageBuffer,
                                                                   Accesses.VertexAttributeRead, Accesses.ShaderWrite,
                                                                   _context.GraphicsQueue.FamilyIndex, _context.ComputeQueue.FamilyIndex);

            var computeToGraphicsBarrier = new BufferMemoryBarrier(_storageBuffer,
                                                                   Accesses.ShaderWrite, Accesses.VertexAttributeRead,
                                                                   _context.ComputeQueue.FamilyIndex, _context.GraphicsQueue.FamilyIndex);

            commandBuffer.Begin();

            commandBuffer.CmdPipelineBarrier(PipelineStages.VertexInput, PipelineStages.ComputeShader,
                                                 bufferMemoryBarriers: new[] { graphicsToComputeBarrier });

            commandBuffer.CmdBindPipeline(PipelineBindPoint.Compute, _pipeline);
            commandBuffer.CmdBindDescriptorSet(PipelineBindPoint.Compute, _pipelineLayout, _descriptorSet);
            commandBuffer.CmdDispatch(_storageBuffer.Count / Threads, 1, 1);

            commandBuffer.CmdPipelineBarrier(PipelineStages.ComputeShader, PipelineStages.VertexInput,
                                                 bufferMemoryBarriers: new[] { computeToGraphicsBarrier });

            commandBuffer.End();
        }
    }
}
