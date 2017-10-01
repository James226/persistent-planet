using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using MemBus;
using PersistentPlanet.Graphics.DirectX11;
using VulkanCore;
using VulkanCore.Ext;
using VulkanCore.Khr;
using Semaphore = VulkanCore.Semaphore;

namespace PersistentPlanet.Graphics.Vulkan
{
    public class VulkanRenderer : IGenericRenderer<VulkanInitialiseContext, VulkanRenderContext>, IDisposable
    {
        private VulkanResourceFactory _resourceFactory;
        private Instance _instance;
        private DebugReportCallbackExt _debugCallback;
        private SurfaceKhr _surface;
        private VulkanContext _context;
        private Semaphore _imageAvailableSemaphore;
        private Semaphore _renderingFinishedSemaphore;
        private SwapchainKhr _swapChain;
        private Image[] _swapChainImages;
        private CommandBuffer[] _commandBuffers;
        private ContentManager _contentManager;
        private RenderPass _renderPass;
        private PipelineLayout _pipelineLayout;
        private ImageView[] _imageViews;
        private Framebuffer[] _framebuffers;
        private Pipeline _pipeline;
        private VulkanBuffer _uniformBuffer;
        private VulkanImage _depthStencilBuffer;

        private Fence[] _fences;
        private IRenderWindow _renderWindow;
        private CommandBuffer _computeCommandBuffer;

        public (VulkanInitialiseContext, Func<VulkanRenderContext>) Initialise(IRenderWindow renderWindow, IBus bus)
        {
            var renderWait = new CountdownLatch();
            _renderWindow = renderWindow;
            _instance = CreateInstance(true);
            _debugCallback = CreateDebugReportCallback(_instance, true);
            _surface = CreateSurface(_instance, renderWindow);
            _context = new VulkanContext(_instance, _surface, renderWait);
            _contentManager = new ContentManager(_context, "Content");
            _imageAvailableSemaphore = _context.Device.CreateSemaphore();
            _renderingFinishedSemaphore = _context.Device.CreateSemaphore();
            _uniformBuffer = VulkanBuffer.DynamicUniform<WorldBuffer>(_context, 1);

            _depthStencilBuffer = VulkanImage.DepthStencil(_context, renderWindow.WindowWidth, renderWindow.WindowHeight);

            _swapChain = CreateSwapchain(_context, _surface);
            _swapChainImages = _swapChain.GetImages();
            

            _renderPass = CreateRenderPass();


            var viewProjection = new WorldBuffer
            {
                View = Matrix4x4.CreateLookAt(-Vector3.UnitZ * 70 + Vector3.UnitY * 50, Vector3.Zero, -Vector3.UnitY),
                Projection = Matrix4x4.CreatePerspectiveFieldOfView(
                    (float)Math.PI / 4,
                    (float)renderWindow.WindowWidth / renderWindow.WindowHeight,
                    1.0f,
                    1000.0f)
            };

            var ptr = _uniformBuffer.Memory.Map(0, Interop.SizeOf<WorldBuffer>());
            Interop.Write(ptr, ref viewProjection);
            _uniformBuffer.Memory.Unmap();

            var initialiseContext = new VulkanInitialiseContext
            {
                Context = _context,
                Content = _contentManager,
                RenderPass = _renderPass,
                RenderWindow = renderWindow,
                WorldBuffer = _uniformBuffer,
                Bus = bus
            };
            _resourceFactory = new VulkanResourceFactory(initialiseContext);

            _pipelineLayout = CreatePipelineLayout();

            _imageViews = CreateImageViews();
            _framebuffers = CreateFramebuffers(_imageViews, _renderPass, renderWindow.WindowWidth, renderWindow.WindowHeight);
            _pipeline = CreateGraphicsPipeline(_pipelineLayout, _renderPass, renderWindow.WindowWidth, renderWindow.WindowHeight);

            _commandBuffers =
                _context.GraphicsCommandPool.AllocateBuffers(
                    new CommandBufferAllocateInfo(CommandBufferLevel.Primary, _swapChainImages.Length));

            _fences = new []
            {
                _context.Device.CreateFence(new FenceCreateInfo(FenceCreateFlags.Signaled)),
                _context.Device.CreateFence(new FenceCreateInfo(FenceCreateFlags.Signaled)),
            };

            _computeCommandBuffer = _context.ComputeCommandPool.AllocateBuffers(new CommandBufferAllocateInfo(CommandBufferLevel.Primary, 1))[0];


            Win32.QueryPerformanceFrequency(out var counterFrequency);
            float ticksPerSecond = counterFrequency;

            Win32.QueryPerformanceCounter(out var lastTimestamp);

            var renderContext = new VulkanRenderContext
            {
                Bus = bus,
                RenderWait = renderWait
            };

            var shader = new ComputeShader();
            shader.Initialise(initialiseContext);
            shader.Record(renderContext, _computeCommandBuffer);
            var processFence = _context.Device.CreateFence();
            _context.ComputeQueue.Submit(new SubmitInfo(commandBuffers: new[] { _computeCommandBuffer }), processFence);
            processFence.Wait();
            processFence.Dispose();
            shader.PrintResults(_context);
            shader.Dispose();

            return (initialiseContext, () =>
                                       { 
                                           Win32.QueryPerformanceCounter(out var timestamp);
                                           renderContext.DeltaTime = (timestamp - lastTimestamp) / ticksPerSecond;
                                           lastTimestamp = timestamp;

                                           return renderContext;
                                       });
        }

        private Instance CreateInstance(bool debug)
        {
            // Specify standard validation layers.
            string surfaceExtension;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                surfaceExtension = Constant.InstanceExtension.KhrWin32Surface;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                surfaceExtension = Constant.InstanceExtension.KhrXlibSurface;
            }
            else
            {
                throw new NotImplementedException();
            }

            var createInfo = new InstanceCreateInfo();
            if (debug)
            {
                var availableLayers = Instance.EnumerateLayerProperties();
                createInfo.EnabledLayerNames = new[] { Constant.InstanceLayer.LunarGStandardValidation }
                    .Where(availableLayers.Contains)
                    .ToArray();
                createInfo.EnabledExtensionNames = new[]
                {
                    Constant.InstanceExtension.KhrSurface,
                    surfaceExtension,
                    Constant.InstanceExtension.ExtDebugReport,
                };
            }
            else
            {
                createInfo.EnabledExtensionNames = new[]
                {
                    Constant.InstanceExtension.KhrSurface,
                    surfaceExtension,
                };
            }
            return new Instance(createInfo);
        }

        private static DebugReportCallbackExt CreateDebugReportCallback(Instance instance, bool debug)
        {
            if (!debug) return null;

            // Attach debug callback.
            var debugReportCreateInfo = new DebugReportCallbackCreateInfoExt(
                DebugReportFlagsExt.All,
                args =>
                {
                    Debug.WriteLine($"[{args.Flags}][{args.LayerPrefix}] {args.Message}");
                    return args.Flags.HasFlag(DebugReportFlagsExt.Error);
                }
            );

            return instance.CreateDebugReportCallbackExt(debugReportCreateInfo);
        }

        private static SurfaceKhr CreateSurface(Instance instance, IRenderWindow renderWindow)
        {
            // Create surface.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return instance.CreateWin32SurfaceKhr(new Win32SurfaceCreateInfoKhr(Process.GetCurrentProcess().Handle, renderWindow.Handle));
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // TODO: Support linux
                //return instance.CreateWin32SurfaceKhr(new XlibSurfaceCreateInfoKhr(Process.GetCurrentProcess().Handle, renderWindow.Handle));
            }
            throw new NotImplementedException();
        }

        private static SwapchainKhr CreateSwapchain(VulkanContext context, SurfaceKhr surface)
        {
            var capabilities = context.PhysicalDevice.GetSurfaceCapabilitiesKhr(surface);
            var formats = context.PhysicalDevice.GetSurfaceFormatsKhr(surface);
            var presentModes = context.PhysicalDevice.GetSurfacePresentModesKhr(surface);
            var format = formats.Length == 1 && formats[0].Format == Format.Undefined
                ? Format.B8G8R8A8UNorm
                : formats[0].Format;
            var presentMode =
                presentModes.Contains(PresentModeKhr.Mailbox) ? PresentModeKhr.Mailbox :
                    presentModes.Contains(PresentModeKhr.FifoRelaxed) ? PresentModeKhr.FifoRelaxed :
                        presentModes.Contains(PresentModeKhr.Fifo) ? PresentModeKhr.Fifo :
                            PresentModeKhr.Immediate;

            return context.Device.CreateSwapchainKhr(new SwapchainCreateInfoKhr(
                                                         surface,
                                                         format,
                                                         capabilities.CurrentExtent,
                                                         capabilities.CurrentTransform,
                                                         presentMode));
        }

        private RenderPass CreateRenderPass()
        {
            var attachments = new[]
            {
                // Color attachment.
                new AttachmentDescription
                {
                    Format = _swapChain.Format,
                    Samples = SampleCounts.Count1,
                    LoadOp = AttachmentLoadOp.Clear,
                    StoreOp = AttachmentStoreOp.Store,
                    StencilLoadOp = AttachmentLoadOp.DontCare,
                    StencilStoreOp = AttachmentStoreOp.DontCare,
                    InitialLayout = ImageLayout.Undefined,
                    FinalLayout = ImageLayout.PresentSrcKhr
                },
                // Depth attachment.
                new AttachmentDescription
                {
                    Format = _depthStencilBuffer.Format,
                    Samples = SampleCounts.Count1,
                    LoadOp = AttachmentLoadOp.Clear,
                    StoreOp = AttachmentStoreOp.DontCare,
                    StencilLoadOp = AttachmentLoadOp.DontCare,
                    StencilStoreOp = AttachmentStoreOp.DontCare,
                    InitialLayout = ImageLayout.Undefined,
                    FinalLayout = ImageLayout.DepthStencilAttachmentOptimal
                }
            };
            var subpasses = new[]
            {
                new SubpassDescription(
                    new[] { new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal) },
                    new AttachmentReference(1, ImageLayout.DepthStencilAttachmentOptimal))
            };
            var dependencies = new[]
            {
                new SubpassDependency
                {
                    SrcSubpass = Constant.SubpassExternal,
                    DstSubpass = 0,
                    SrcStageMask = PipelineStages.BottomOfPipe,
                    DstStageMask = PipelineStages.ColorAttachmentOutput,
                    SrcAccessMask = Accesses.MemoryRead,
                    DstAccessMask = Accesses.ColorAttachmentRead | Accesses.ColorAttachmentWrite,
                    DependencyFlags = Dependencies.ByRegion
                },
                new SubpassDependency
                {
                    SrcSubpass = 0,
                    DstSubpass = Constant.SubpassExternal,
                    SrcStageMask = PipelineStages.ColorAttachmentOutput,
                    DstStageMask = PipelineStages.BottomOfPipe,
                    SrcAccessMask = Accesses.ColorAttachmentRead | Accesses.ColorAttachmentWrite,
                    DstAccessMask = Accesses.MemoryRead,
                    DependencyFlags = Dependencies.ByRegion
                }
            };

            var createInfo = new RenderPassCreateInfo(subpasses, attachments, dependencies);
            return _context.Device.CreateRenderPass(createInfo);
        }

        private ImageView[] CreateImageViews()
        {
            var imageViews = new ImageView[_swapChainImages.Length];
            for (int i = 0; i < _swapChainImages.Length; i++)
            {
                imageViews[i] = _swapChainImages[i].CreateView(new ImageViewCreateInfo(
                    _swapChain.Format,
                    new ImageSubresourceRange(ImageAspects.Color, 0, 1, 0, 1)));
            }
            return imageViews;
        }

        private Framebuffer[] CreateFramebuffers(ImageView[] imageViews, RenderPass renderPass, int width, int height)
        {
            var framebuffers = new Framebuffer[_swapChainImages.Length];
            for (var i = 0; i < _swapChainImages.Length; i++)
            {
                framebuffers[i] = renderPass.CreateFramebuffer(new FramebufferCreateInfo(
                    new[] { imageViews[i], _depthStencilBuffer.View },
                    width,
                    height));
            }
            return framebuffers;
        }

        private PipelineLayout CreatePipelineLayout()
        {
            var layoutCreateInfo = new PipelineLayoutCreateInfo();
            return _context.Device.CreatePipelineLayout(layoutCreateInfo);
        }

        private Pipeline CreateGraphicsPipeline(PipelineLayout pipelineLayout, RenderPass renderPass, int width, int height)
        {
            ShaderModule vertexShader = _contentManager.Load<ShaderModule>("triangle.vert.spv");
            ShaderModule fragmentShader = _contentManager.Load<ShaderModule>("triangle.frag.spv");
            var shaderStageCreateInfos = new[]
            {
                new PipelineShaderStageCreateInfo(ShaderStages.Vertex, vertexShader, "main"),
                new PipelineShaderStageCreateInfo(ShaderStages.Fragment, fragmentShader, "main")
            };

            var vertexInputStateCreateInfo = new PipelineVertexInputStateCreateInfo();
            var inputAssemblyStateCreateInfo = new PipelineInputAssemblyStateCreateInfo(PrimitiveTopology.TriangleList);
            var viewportStateCreateInfo = new PipelineViewportStateCreateInfo(
                new Viewport(0, 0, width, height),
                new Rect2D(0, 0, width, height));
            var rasterizationStateCreateInfo = new PipelineRasterizationStateCreateInfo
            {
                PolygonMode = PolygonMode.Fill,
                CullMode = CullModes.Back,
                FrontFace = FrontFace.CounterClockwise,
                LineWidth = 1.0f
            };
            var multisampleStateCreateInfo = new PipelineMultisampleStateCreateInfo
            {
                RasterizationSamples = SampleCounts.Count1,
                MinSampleShading = 1.0f
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
            var pipelineCreateInfo = new GraphicsPipelineCreateInfo(
                pipelineLayout, renderPass, 0,
                shaderStageCreateInfos,
                inputAssemblyStateCreateInfo,
                vertexInputStateCreateInfo,
                rasterizationStateCreateInfo,
                viewportState: viewportStateCreateInfo,
                multisampleState: multisampleStateCreateInfo,
                depthStencilState: depthStencilCreateInfo,
                colorBlendState: colorBlendStateCreateInfo);
            return _context.Device.CreateGraphicsPipeline(pipelineCreateInfo);
        }

        public void RecordCommandBuffers(int width, int height, int i, Action record, VulkanRenderContext context)
        {
            var subresourceRange = new ImageSubresourceRange(ImageAspects.Color, 0, 1, 0, 1);
            CommandBuffer cmdBuffer = _commandBuffers[i];

            cmdBuffer.Begin(new CommandBufferBeginInfo(CommandBufferUsages.SimultaneousUse));

            if (_context.PresentQueue != _context.GraphicsQueue)
            {
                var barrierFromPresentToDraw = new ImageMemoryBarrier(
                    _swapChainImages[i], subresourceRange,
                    Accesses.MemoryRead, Accesses.ColorAttachmentWrite,
                    ImageLayout.Undefined, ImageLayout.PresentSrcKhr,
                    _context.PresentQueue.FamilyIndex, _context.GraphicsQueue.FamilyIndex);

                cmdBuffer.CmdPipelineBarrier(
                    PipelineStages.ColorAttachmentOutput,
                    PipelineStages.ColorAttachmentOutput,
                    imageMemoryBarriers: new[] { barrierFromPresentToDraw });
            }

            var renderPassBeginInfo = new RenderPassBeginInfo(
                _framebuffers[i],
                new Rect2D(Offset2D.Zero, new Extent2D(width, height)),
                new ClearColorValue(new ColorF4(0.39f, 0.58f, 0.93f, 1.0f)),
                new ClearDepthStencilValue(1.0f, 0));
            cmdBuffer.CmdBeginRenderPass(renderPassBeginInfo);
            cmdBuffer.CmdBindPipeline(PipelineBindPoint.Graphics, _pipeline);
            //cmdBuffer.CmdDraw(3);

            context.CommandBuffer = cmdBuffer;
            record.Invoke();
            //RecordCommandBuffer(cmdBuffer, i, width, height);

            cmdBuffer.CmdEndRenderPass();

            if (_context.PresentQueue != _context.GraphicsQueue)
            {
                var barrierFromDrawToPresent = new ImageMemoryBarrier(
                    _swapChainImages[i], subresourceRange,
                    Accesses.ColorAttachmentWrite, Accesses.MemoryRead,
                    ImageLayout.PresentSrcKhr, ImageLayout.PresentSrcKhr,
                    _context.GraphicsQueue.FamilyIndex, _context.PresentQueue.FamilyIndex);

                cmdBuffer.CmdPipelineBarrier(
                    PipelineStages.ColorAttachmentOutput,
                    PipelineStages.BottomOfPipe,
                    imageMemoryBarriers: new[] { barrierFromDrawToPresent });
            }

            cmdBuffer.End();
        }

        protected void RecordCommandBuffer(CommandBuffer cmdBuffer, int imageIndex, int width, int height)
        {
            

        }

        public void Dispose()
        {
            _instance?.Dispose();
            _debugCallback?.Dispose();
            _surface?.Dispose();
            _context?.Dispose();
            _imageAvailableSemaphore?.Dispose();
            _renderingFinishedSemaphore?.Dispose();
            _depthStencilBuffer?.Dispose();
            _uniformBuffer?.Dispose();
            _swapChain?.Dispose();
            _contentManager?.Dispose();
            _renderPass?.Dispose();
            _pipelineLayout?.Dispose();
            _pipeline?.Dispose();
        }

        public Scene<VulkanRenderContext> CreateScene()
        {
            return new Scene<VulkanRenderContext>(_resourceFactory);
        }

        public void Render(VulkanRenderContext context, Action render)
        {
            int imageIndex = _swapChain.AcquireNextImage(semaphore: _imageAvailableSemaphore);

            _context.Device.WaitFences(new [] { _fences[imageIndex] }, true);
            _fences[imageIndex].Reset();
            context.RenderWait.WaitUntilZero();

            RecordCommandBuffers(_renderWindow.WindowWidth, _renderWindow.WindowHeight, imageIndex, render, context);

            // Submit recorded commands to graphics queue for execution.
            _context.GraphicsQueue.Submit(
                _imageAvailableSemaphore,
                PipelineStages.ColorAttachmentOutput,
                _commandBuffers[imageIndex],
                _renderingFinishedSemaphore,
                _fences[imageIndex]
            );

            // Present the color output to screen.
            _context.PresentQueue.PresentKhr(_renderingFinishedSemaphore, _swapChain, imageIndex);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WorldBuffer
    {
        public Matrix4x4 View;
        public Matrix4x4 Projection;
    }
}