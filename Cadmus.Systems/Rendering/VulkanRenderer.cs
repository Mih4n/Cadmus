using System.Numerics;
using Cadmus.Domain.Components;
using Cadmus.Domain.Contracts.Game;
using Cadmus.Domain.Contracts.Systems;
using Cadmus.Render;
using Silk.NET.Vulkan;

namespace Cadmus.Systems.Rendering;

public unsafe class VulkanRenderer : IRenderer
{
    private readonly IGameContext _context;
    private VulkanRenderingContext _vkContext = null!;
    private VulkanPipeline _pipeline = null!;
    private VulkanUniformBuffer _ubo = null!;
    private DescriptorSet[] _descriptorSets = [];
    private bool _initialized;

    private readonly Dictionary<MeshComponent, VulkanMesh> _meshCache = [];
    private readonly Dictionary<MaterialComponent, VulkanTexture> _textureCache = [];

    public VulkanRenderer(IGameContext context)
    {
        _context = context;
    }

    public Task Update(IGameContext gameContext)
    {
        if (!_initialized)
        {
            _vkContext = gameContext.Game.GetComponent<VulkanRenderingContext>()!;
            _pipeline = new VulkanPipeline(_vkContext.Vulkan, _vkContext.Device!, _vkContext.RenderPass!, _vkContext.Swapchain!.Extent,
                Path.Combine(AppContext.BaseDirectory, "Assets/Shaders/sprite.vert.spv"),
                Path.Combine(AppContext.BaseDirectory, "Assets/Shaders/sprite.frag.spv"));
            _ubo = new VulkanUniformBuffer(_vkContext.Vulkan, _vkContext.Device!);
            _descriptorSets = _pipeline.AllocateDescriptorSets();
            _initialized = true;
        }

        var scene = gameContext.Scene;
        if (scene == null) return Task.CompletedTask;

        foreach (var (_, entity) in scene.Entities)
        {
            if (entity.TryGetComponent<MeshComponent>(out var meshComp) && !_meshCache.ContainsKey(meshComp))
            {
                _meshCache[meshComp] = new VulkanMesh(_vkContext.Vulkan, _vkContext.Device!, meshComp.Mesh);
            }

            if (entity.TryGetComponent<MaterialComponent>(out var matComp) && !_textureCache.ContainsKey(matComp))
            {
                var path = Path.Combine(AppContext.BaseDirectory, matComp.TexturePath);
                if (File.Exists(path))
                {
                    _textureCache[matComp] = new VulkanTexture(_vkContext.Vulkan, _vkContext.Device!, path);
                }
            }
        }

        return Task.CompletedTask;
    }

    public void Render()
    {
        if (!_initialized) return;

        var vk = _vkContext.Vulkan;
        var device = _vkContext.Device!;
        var swapchain = _vkContext.Swapchain!;
        var frameSync = _vkContext.FrameSync!;
        var cmdBuffers = _vkContext.CommandBuffers!;
        uint frame = _vkContext.CurrentFrame;

        if (_vkContext.FramebufferResized)
        {
            _vkContext.FramebufferResized = false;
            _vkContext.RecreateSwapchain();
        }

        vk.WaitForFences(device.Device, 1, in frameSync.InFlightFences[frame], true, ulong.MaxValue);
        vk.ResetFences(device.Device, 1, in frameSync.InFlightFences[frame]);

        uint imageIndex = 0;
        var result = _vkContext.KhrSwapchain!.AcquireNextImage(device.Device, swapchain.Swapchain, ulong.MaxValue, frameSync.ImageAvailableSemaphores[frame], default, ref imageIndex);

        if (result == Result.ErrorOutOfDateKhr)
        {
            _vkContext.RecreateSwapchain();
            return;
        }
        else if (result != Result.Success && result != Result.SuboptimalKhr)
        {
            throw new Exception("Failed to acquire swap chain image!");
        }

        var cmdBuffer = cmdBuffers.Buffers[frame];
        vk.ResetCommandBuffer(cmdBuffer, CommandBufferResetFlags.None);

        CommandBufferBeginInfo beginInfo = new() { SType = StructureType.CommandBufferBeginInfo };
        vk.BeginCommandBuffer(cmdBuffer, in beginInfo);

        RenderPassBeginInfo renderPassInfo = new()
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = _vkContext.RenderPass!.RenderPass,
            Framebuffer = swapchain.Framebuffers[imageIndex],
            RenderArea = new Rect2D { Offset = new Offset2D { X = 0, Y = 0 }, Extent = swapchain.Extent }
        };

        var clearValues = stackalloc ClearValue[]
        {
            new ClearValue { Color = new ClearColorValue { Float32_0 = 0, Float32_1 = 0, Float32_2 = 0, Float32_3 = 1 } },
            new ClearValue { DepthStencil = new ClearDepthStencilValue { Depth = 1, Stencil = 0 } }
        };
        renderPassInfo.ClearValueCount = 2;
        renderPassInfo.PClearValues = clearValues;

        vk.CmdBeginRenderPass(cmdBuffer, in renderPassInfo, SubpassContents.Inline);
        vk.CmdBindPipeline(cmdBuffer, PipelineBindPoint.Graphics, _pipeline.GraphicsPipeline);

        Viewport viewport = new()
        {
            X = 0,
            Y = 0,
            Width = swapchain.Extent.Width,
            Height = swapchain.Extent.Height,
            MinDepth = 0,
            MaxDepth = 1
        };
        vk.CmdSetViewport(cmdBuffer, 0, 1, in viewport);

        Rect2D scissor = new()
        {
            Offset = new Offset2D { X = 0, Y = 0 },
            Extent = swapchain.Extent
        };
        vk.CmdSetScissor(cmdBuffer, 0, 1, in scissor);

        Matrix4x4 viewProj = Matrix4x4.Identity;
        var scene = _context.Scene;
        if (scene != null)
        {
            foreach (var (_, e) in scene.Entities)
            {
                if (e.TryGetComponent<CameraComponent>(out var cam))
                {
                    var aspect = (float)swapchain.Extent.Width / swapchain.Extent.Height;
                    viewProj = cam.GetViewMatrix() * cam.GetProjectionMatrix(aspect);
                    break;
                }
            }
        }

        if (scene != null)
        {
            foreach (var (_, entity) in scene.Entities)
            {
                if (!entity.TryGetComponent<TransformComponent>(out var transform)) continue;
                if (!entity.TryGetComponent<MeshComponent>(out var mesh)) continue;
                if (!entity.TryGetComponent<MaterialComponent>(out var material)) continue;
                if (!_meshCache.TryGetValue(mesh, out var gpuMesh)) continue;
                if (!_textureCache.TryGetValue(material, out var gpuTexture)) continue;

                var ubo = new UniformBufferObject
                {
                    ViewProj = viewProj,
                    Model = transform.GetModelMatrix()
                };
                _ubo.Update(ubo);

                _pipeline.UpdateDescriptorSets(_descriptorSets, _ubo, gpuTexture);

                var vertexBuffers = stackalloc Silk.NET.Vulkan.Buffer[] { gpuMesh.VertexBuffer.Buffer };
                var offsets = stackalloc ulong[] { 0 };
                vk.CmdBindVertexBuffers(cmdBuffer, 0, 1, vertexBuffers, offsets);
                vk.CmdBindIndexBuffer(cmdBuffer, gpuMesh.IndexBuffer.Buffer, 0, IndexType.Uint16);

                var sets = stackalloc DescriptorSet[] { _descriptorSets[0], _descriptorSets[1] };
                vk.CmdBindDescriptorSets(cmdBuffer, PipelineBindPoint.Graphics, _pipeline.PipelineLayout, 0, 2, sets, 0, null);

                vk.CmdDrawIndexed(cmdBuffer, gpuMesh.IndexCount, 1, 0, 0, 0);
            }
        }

        vk.CmdEndRenderPass(cmdBuffer);
        vk.EndCommandBuffer(cmdBuffer);

        var waitSemaphores = stackalloc Silk.NET.Vulkan.Semaphore[] { frameSync.ImageAvailableSemaphores[frame] };
        var signalSemaphores = stackalloc Silk.NET.Vulkan.Semaphore[] { frameSync.RenderFinishedSemaphores[frame] };
        var waitStages = stackalloc[] { PipelineStageFlags.ColorAttachmentOutputBit };

        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = waitSemaphores,
            PWaitDstStageMask = waitStages,
            CommandBufferCount = 1,
            PCommandBuffers = &cmdBuffer,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = signalSemaphores
        };

        vk.QueueSubmit(device.GraphicsQueue, 1, in submitInfo, frameSync.InFlightFences[frame]);

        var swapchains = stackalloc SwapchainKHR[] { swapchain.Swapchain };
        var presentInfo = new PresentInfoKHR
        {
            SType = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = signalSemaphores,
            SwapchainCount = 1,
            PSwapchains = swapchains,
            PImageIndices = &imageIndex
        };

        result = _vkContext.KhrSwapchain!.QueuePresent(device.PresentQueue, in presentInfo);

        if (result == Result.ErrorOutOfDateKhr || result == Result.SuboptimalKhr || _vkContext.FramebufferResized)
        {
            _vkContext.FramebufferResized = false;
            _vkContext.RecreateSwapchain();
        }
        else if (result != Result.Success)
        {
            throw new Exception("Failed to present swap chain image!");
        }

        _vkContext.CurrentFrame = (frame + 1) % (uint)frameSync.InFlightFences.Length;
    }

    public void Dispose()
    {
        foreach (var mesh in _meshCache.Values) mesh.Dispose();
        foreach (var tex in _textureCache.Values) tex.Dispose();
        _pipeline?.Dispose();
        _ubo?.Dispose();
    }
}
