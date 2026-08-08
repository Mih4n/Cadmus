using System.Numerics;
using Cadmus.Core.Game;
using Cadmus.Core.Scenes;
using Cadmus.Core.Systems;
using Cadmus.Core.Windowing;
using Cadmus.Engine.Components;
using Cadmus.Engine.Diagnostics;
using Cadmus.Graphics.Resources;
using Cadmus.Graphics.Vulkan;
using Cadmus.Graphics;
using Silk.NET.Vulkan;

namespace Cadmus.Rendering;

/// <summary>
/// Records and submits one command buffer per frame. Every Vulkan object it needs is injected —
/// the system neither creates nor owns them, so swapchain recreation and disposal stay in one place.
/// </summary>
public sealed unsafe class VulkanRenderSystem : IRenderSystem, IDisposable
{
    private readonly VulkanDevice device;
    private readonly VulkanSwapchain swapchain;
    private readonly VulkanRenderPass renderPass;
    private readonly VulkanFramebuffers framebuffers;
    private readonly VulkanFrameSync sync;
    private readonly VulkanCommandBuffers commandBuffers;
    private readonly VulkanPipeline pipeline;
    private readonly UniformRing uniforms;
    private readonly IGpuResourceCache resources;
    private readonly ISceneManager scenes;
    private readonly IGameWindow window;
    private readonly VulkanOptions options;
    private readonly RenderItemCollector collector;
    private readonly CameraComponent defaultCamera = new();
    private readonly Vk vk;

    private readonly IFrameCapture capture;
    private readonly DebugOverlay overlay;
    private readonly FrameStatistics statistics;

    private IReadOnlyList<RenderItem> items = [];
    private IReadOnlyList<RenderItem> overlayItems = [];
    private uint currentFrame;
    private bool framebufferResized;
    private bool disposed;
    private string? pendingCapturePath;

    /// <summary>Runs after gameplay systems so it sees the state they just produced.</summary>
    public int Order => int.MaxValue;

    public VulkanRenderSystem(
        VulkanDevice device,
        VulkanSwapchain swapchain,
        VulkanRenderPass renderPass,
        VulkanFramebuffers framebuffers,
        VulkanFrameSync sync,
        VulkanCommandBuffers commandBuffers,
        VulkanPipeline pipeline,
        UniformRing uniforms,
        IGpuResourceCache resources,
        ISceneManager scenes,
        IGameWindow window,
        VulkanOptions options,
        RenderItemCollector collector,
        IFrameCapture capture,
        DebugOverlay overlay,
        FrameStatistics statistics)
    {
        this.capture = capture;
        this.overlay = overlay;
        this.statistics = statistics;
        this.device = device;
        this.swapchain = swapchain;
        this.renderPass = renderPass;
        this.framebuffers = framebuffers;
        this.sync = sync;
        this.commandBuffers = commandBuffers;
        this.pipeline = pipeline;
        this.uniforms = uniforms;
        this.resources = resources;
        this.scenes = scenes;
        this.window = window;
        this.options = options;
        this.collector = collector;
        vk = device.Api;

        window.Resized += OnResized;
    }

    private void OnResized((int Width, int Height) size) => framebufferResized = true;

    public ValueTask UpdateAsync(GameTime time, CancellationToken cancellationToken = default)
    {
        items = collector.Collect(scenes.Current);

        // Published before the overlay is built, so the HUD reports the frame it is drawn on and
        // never counts its own glyphs as draw calls.
        statistics.DrawCalls = items.Count;
        statistics.Resolution = ((int)swapchain.Extent.Width, (int)swapchain.Extent.Height);
        statistics.CachedTextures = resources.TextureCount;
        statistics.CachedMeshes = resources.MeshCount;
        statistics.DeviceName = device.Name;

        overlayItems = overlay.Build((int)swapchain.Extent.Width, (int)swapchain.Extent.Height);

        return ValueTask.CompletedTask;
    }

    public void Render(GameTime time)
    {
        if (window.IsMinimized)
        {
            return;
        }

        var frame = currentFrame;

        vk.WaitForFences(device.Handle, 1, in sync.InFlight[frame], true, ulong.MaxValue);

        uint imageIndex = 0;
        var acquireResult = device.SwapchainApi.AcquireNextImage(
            device.Handle, swapchain.Handle, ulong.MaxValue, sync.ImageAvailable[frame], default, ref imageIndex);

        if (acquireResult == Result.ErrorOutOfDateKhr)
        {
            swapchain.Recreate();
            return;
        }

        if (acquireResult != Result.Success && acquireResult != Result.SuboptimalKhr)
        {
            throw new InvalidOperationException($"Failed to acquire a swapchain image: {acquireResult}.");
        }

        // Reset only once we know we will submit, or the fence is never signalled again.
        vk.ResetFences(device.Handle, 1, in sync.InFlight[frame]);

        var commandBuffer = commandBuffers.Buffers[frame];
        vk.ResetCommandBuffer(commandBuffer, CommandBufferResetFlags.None);

        RecordCommandBuffer(commandBuffer, imageIndex);

        var renderFinished = sync.RenderFinished[imageIndex % (uint)sync.RenderFinished.Length];
        Submit(commandBuffer, frame, renderFinished);

        // Must happen before presenting: once the image is handed to the presentation engine it is
        // no longer acquired, and transitioning its layout would be a spec violation.
        if (pendingCapturePath is { } capturePath)
        {
            pendingCapturePath = null;
            capture.Capture(imageIndex, capturePath);
        }

        Present(imageIndex, renderFinished);

        currentFrame = (frame + 1) % sync.FramesInFlight;
    }

    /// <summary>Saves the next presented frame to <paramref name="path"/> as PNG.</summary>
    public void RequestCapture(string path) => pendingCapturePath = path;

    private void RecordCommandBuffer(CommandBuffer commandBuffer, uint imageIndex)
    {
        CommandBufferBeginInfo beginInfo = new() { SType = StructureType.CommandBufferBeginInfo };
        vk.BeginCommandBuffer(commandBuffer, in beginInfo);

        var clearValues = stackalloc ClearValue[]
        {
            new ClearValue
            {
                Color = new ClearColorValue
                {
                    Float32_0 = options.ClearColor.X,
                    Float32_1 = options.ClearColor.Y,
                    Float32_2 = options.ClearColor.Z,
                    Float32_3 = options.ClearColor.W
                }
            },
            new ClearValue { DepthStencil = new ClearDepthStencilValue { Depth = 1, Stencil = 0 } }
        };

        RenderPassBeginInfo renderPassInfo = new()
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = renderPass.Handle,
            Framebuffer = framebuffers.Handles[imageIndex],
            RenderArea = new Rect2D { Offset = new Offset2D { X = 0, Y = 0 }, Extent = swapchain.Extent },
            ClearValueCount = 2,
            PClearValues = clearValues
        };

        vk.CmdBeginRenderPass(commandBuffer, in renderPassInfo, SubpassContents.Inline);
        vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, pipeline.Handle);

        Viewport viewport = new()
        {
            X = 0,
            Y = 0,
            Width = swapchain.Extent.Width,
            Height = swapchain.Extent.Height,
            MinDepth = 0,
            MaxDepth = 1
        };
        vk.CmdSetViewport(commandBuffer, 0, 1, in viewport);

        Rect2D scissor = new()
        {
            Offset = new Offset2D { X = 0, Y = 0 },
            Extent = swapchain.Extent
        };
        vk.CmdSetScissor(commandBuffer, 0, 1, in scissor);

        DrawItems(commandBuffer);

        vk.CmdEndRenderPass(commandBuffer);
        vk.EndCommandBuffer(commandBuffer);
    }

    private void DrawItems(CommandBuffer commandBuffer)
    {
        if (items.Count == 0 && overlayItems.Count == 0)
        {
            return;
        }

        var viewProjection = GetViewProjection();
        var screenProjection = ScreenProjection;
        uniforms.BeginFrame(currentFrame);

        // Allocated once for the whole loop: stackalloc inside a loop is not released per iteration,
        // so a few thousand draws would walk off the stack.
        var offsets = stackalloc ulong[] { 0 };
        var sets = stackalloc DescriptorSet[2];
        var vertexBuffers = stackalloc Silk.NET.Vulkan.Buffer[1];

        sets[0] = uniforms.CurrentSet;

        Draw(items);
        Draw(overlayItems);

        void Draw(IReadOnlyList<RenderItem> list)
        {
        foreach (var item in list)
        {
            var data = new ObjectUniforms
            {
                ViewProjection = item.ScreenSpace ? screenProjection : viewProjection,
                Model = item.Model,
                Tint = item.Tint
            };

            if (!uniforms.TryPush(in data, out var dynamicOffset))
            {
                // Budget exhausted: drop the rest of the frame rather than corrupt earlier draws.
                break;
            }

            var mesh = resources.GetMesh(item.Mesh);
            sets[1] = resources.GetTextureDescriptor(item.TexturePath);

            vk.CmdBindDescriptorSets(
                commandBuffer, PipelineBindPoint.Graphics, pipeline.Layout, 0, 2, sets, 1, &dynamicOffset);

            vertexBuffers[0] = mesh.VertexBuffer.Handle;
            vk.CmdBindVertexBuffers(commandBuffer, 0, 1, vertexBuffers, offsets);
            vk.CmdBindIndexBuffer(commandBuffer, mesh.IndexBuffer.Handle, 0, IndexType.Uint16);

            vk.CmdDrawIndexed(commandBuffer, mesh.IndexCount, 1, 0, 0, 0);
        }
        }
    }

    /// <summary>Pixel-space projection of the framebuffer, independent of the scene camera.</summary>
    private Matrix4x4 ScreenProjection => Matrix4x4.CreateOrthographicOffCenter(
        0,
        swapchain.Extent.Width,
        0,
        swapchain.Extent.Height,
        -2000f,
        2000f
    );

    private Matrix4x4 GetViewProjection()
    {
        var width = (int)swapchain.Extent.Width;
        var height = (int)swapchain.Extent.Height;

        var scene = scenes.Current;
        if (scene is not null)
        {
            foreach (var (_, entity) in scene.Entities)
            {
                if (entity.TryGetComponent<CameraComponent>(out var camera))
                {
                    return camera.GetViewProjection(width, height);
                }
            }
        }

        // No camera in the scene: fall back to a pixel-space view of the whole window.
        return defaultCamera.GetViewProjection(width, height);
    }

    private void Submit(CommandBuffer commandBuffer, uint frame, Silk.NET.Vulkan.Semaphore renderFinished)
    {
        var waitSemaphores = stackalloc Silk.NET.Vulkan.Semaphore[] { sync.ImageAvailable[frame] };
        var signalSemaphores = stackalloc Silk.NET.Vulkan.Semaphore[] { renderFinished };
        var waitStages = stackalloc PipelineStageFlags[] { PipelineStageFlags.ColorAttachmentOutputBit };

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = waitSemaphores,
            PWaitDstStageMask = waitStages,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = signalSemaphores
        };

        if (vk.QueueSubmit(device.GraphicsQueue, 1, in submitInfo, sync.InFlight[frame]) != Result.Success)
        {
            throw new InvalidOperationException("Failed to submit the draw command buffer.");
        }
    }

    private void Present(uint imageIndex, Silk.NET.Vulkan.Semaphore renderFinished)
    {
        var waitSemaphores = stackalloc Silk.NET.Vulkan.Semaphore[] { renderFinished };
        var swapchains = stackalloc SwapchainKHR[] { swapchain.Handle };

        PresentInfoKHR presentInfo = new()
        {
            SType = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = waitSemaphores,
            SwapchainCount = 1,
            PSwapchains = swapchains,
            PImageIndices = &imageIndex
        };

        var result = device.SwapchainApi.QueuePresent(device.PresentQueue, in presentInfo);

        if (result is Result.ErrorOutOfDateKhr or Result.SuboptimalKhr || framebufferResized)
        {
            framebufferResized = false;
            swapchain.Recreate();
        }
        else if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to present a swapchain image: {result}.");
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        window.Resized -= OnResized;
        device.WaitIdle();
        disposed = true;
    }
}
