using Cadmus.Graphics.Resources;
using Cadmus.Graphics.Vulkan;
using Cadmus.Graphics;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Cadmus.Graphics.Vulkan;

/// <summary>
/// The sprite pipeline: set 0 holds a <em>dynamic</em> uniform buffer (one slot per draw, selected
/// at bind time), set 1 holds the texture. Dynamic uniforms need no shader change — GLSL declares
/// both descriptor types identically — which is why the shipped SPIR-V still applies.
/// </summary>
public sealed unsafe class VulkanPipeline : IDisposable
{
    private readonly VulkanDevice device;

    public DescriptorSetLayout UniformSetLayout { get; }
    public DescriptorSetLayout TextureSetLayout { get; }
    public PipelineLayout Layout { get; }
    public Pipeline Handle { get; }
    public DescriptorPool DescriptorPool { get; }

    public VulkanPipeline(VulkanDevice device, VulkanRenderPass renderPass, VulkanOptions options)
    {
        this.device = device;

        // Visible to both stages: the vertex shader reads the matrices, the fragment shader the tint.
        UniformSetLayout = CreateSetLayout(
            DescriptorType.UniformBufferDynamic,
            ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit
        );
        TextureSetLayout = CreateSetLayout(DescriptorType.CombinedImageSampler, ShaderStageFlags.FragmentBit);

        (Layout, Handle) = CreateGraphicsPipeline(renderPass, options);
        DescriptorPool = CreateDescriptorPool(options);
    }

    private DescriptorSetLayout CreateSetLayout(DescriptorType type, ShaderStageFlags stages)
    {
        DescriptorSetLayoutBinding binding = new()
        {
            Binding = 0,
            DescriptorType = type,
            DescriptorCount = 1,
            StageFlags = stages
        };

        DescriptorSetLayoutCreateInfo layoutInfo = new()
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = 1,
            PBindings = &binding
        };

        if (device.Api.CreateDescriptorSetLayout(device.Handle, in layoutInfo, null, out var layout) != Result.Success)
        {
            throw new InvalidOperationException($"Failed to create the {type} descriptor set layout.");
        }

        return layout;
    }

    private (PipelineLayout, Pipeline) CreateGraphicsPipeline(VulkanRenderPass renderPass, VulkanOptions options)
    {
        using var vertexShader = VulkanShaderModule.FromFile(device, VulkanOptions.ResolvePath(options.VertexShaderPath));
        using var fragmentShader = VulkanShaderModule.FromFile(device, VulkanOptions.ResolvePath(options.FragmentShaderPath));

        var entryPoint = (byte*)SilkMarshal.StringToPtr("main");

        var shaderStages = stackalloc PipelineShaderStageCreateInfo[]
        {
            new()
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.VertexBit,
                Module = vertexShader.Handle,
                PName = entryPoint
            },
            new()
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.FragmentBit,
                Module = fragmentShader.Handle,
                PName = entryPoint
            }
        };

        var bindingDescription = Vertex.GetBindingDescription();
        var attributeDescriptions = Vertex.GetAttributeDescriptions();

        try
        {
            fixed (VertexInputAttributeDescription* pAttributes = attributeDescriptions)
            {
                PipelineVertexInputStateCreateInfo vertexInput = new()
                {
                    SType = StructureType.PipelineVertexInputStateCreateInfo,
                    VertexBindingDescriptionCount = 1,
                    PVertexBindingDescriptions = &bindingDescription,
                    VertexAttributeDescriptionCount = (uint)attributeDescriptions.Length,
                    PVertexAttributeDescriptions = pAttributes
                };

                PipelineInputAssemblyStateCreateInfo inputAssembly = new()
                {
                    SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                    Topology = PrimitiveTopology.TriangleList,
                    PrimitiveRestartEnable = false
                };

                // Viewport and scissor are dynamic; these placeholders only satisfy the create info.
                Viewport viewport = new() { Width = 1, Height = 1, MinDepth = 0, MaxDepth = 1 };
                Rect2D scissor = new() { Extent = new Extent2D { Width = 1, Height = 1 } };

                PipelineViewportStateCreateInfo viewportState = new()
                {
                    SType = StructureType.PipelineViewportStateCreateInfo,
                    ViewportCount = 1,
                    PViewports = &viewport,
                    ScissorCount = 1,
                    PScissors = &scissor
                };

                PipelineRasterizationStateCreateInfo rasterizer = new()
                {
                    SType = StructureType.PipelineRasterizationStateCreateInfo,
                    DepthClampEnable = false,
                    RasterizerDiscardEnable = false,
                    PolygonMode = PolygonMode.Fill,
                    LineWidth = 1,
                    // 2D quads are viewed from both sides depending on the projection's handedness.
                    CullMode = CullModeFlags.None,
                    FrontFace = FrontFace.CounterClockwise,
                    DepthBiasEnable = false
                };

                PipelineMultisampleStateCreateInfo multisampling = new()
                {
                    SType = StructureType.PipelineMultisampleStateCreateInfo,
                    SampleShadingEnable = false,
                    RasterizationSamples = SampleCountFlags.Count1Bit
                };

                PipelineDepthStencilStateCreateInfo depthStencil = new()
                {
                    SType = StructureType.PipelineDepthStencilStateCreateInfo,
                    DepthTestEnable = true,
                    DepthWriteEnable = true,
                    DepthCompareOp = CompareOp.LessOrEqual,
                    DepthBoundsTestEnable = false,
                    StencilTestEnable = false
                };

                PipelineColorBlendAttachmentState colorBlendAttachment = new()
                {
                    ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit
                                   | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
                    BlendEnable = true,
                    SrcColorBlendFactor = BlendFactor.SrcAlpha,
                    DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
                    ColorBlendOp = BlendOp.Add,
                    SrcAlphaBlendFactor = BlendFactor.One,
                    DstAlphaBlendFactor = BlendFactor.OneMinusSrcAlpha,
                    AlphaBlendOp = BlendOp.Add
                };

                PipelineColorBlendStateCreateInfo colorBlending = new()
                {
                    SType = StructureType.PipelineColorBlendStateCreateInfo,
                    LogicOpEnable = false,
                    AttachmentCount = 1,
                    PAttachments = &colorBlendAttachment
                };

                var dynamicStates = stackalloc DynamicState[] { DynamicState.Viewport, DynamicState.Scissor };
                PipelineDynamicStateCreateInfo dynamicState = new()
                {
                    SType = StructureType.PipelineDynamicStateCreateInfo,
                    DynamicStateCount = 2,
                    PDynamicStates = dynamicStates
                };

                var setLayouts = stackalloc DescriptorSetLayout[] { UniformSetLayout, TextureSetLayout };
                PipelineLayoutCreateInfo layoutInfo = new()
                {
                    SType = StructureType.PipelineLayoutCreateInfo,
                    SetLayoutCount = 2,
                    PSetLayouts = setLayouts
                };

                if (device.Api.CreatePipelineLayout(device.Handle, in layoutInfo, null, out var pipelineLayout) != Result.Success)
                {
                    throw new InvalidOperationException("Failed to create the pipeline layout.");
                }

                GraphicsPipelineCreateInfo pipelineInfo = new()
                {
                    SType = StructureType.GraphicsPipelineCreateInfo,
                    StageCount = 2,
                    PStages = shaderStages,
                    PVertexInputState = &vertexInput,
                    PInputAssemblyState = &inputAssembly,
                    PViewportState = &viewportState,
                    PRasterizationState = &rasterizer,
                    PMultisampleState = &multisampling,
                    PDepthStencilState = &depthStencil,
                    PColorBlendState = &colorBlending,
                    PDynamicState = &dynamicState,
                    Layout = pipelineLayout,
                    RenderPass = renderPass.Handle,
                    Subpass = 0,
                    BasePipelineHandle = default
                };

                if (device.Api.CreateGraphicsPipelines(device.Handle, default, 1, in pipelineInfo, null, out var pipeline) != Result.Success)
                {
                    throw new InvalidOperationException("Failed to create the graphics pipeline.");
                }

                return (pipelineLayout, pipeline);
            }
        }
        finally
        {
            SilkMarshal.Free((nint)entryPoint);
        }
    }

    private DescriptorPool CreateDescriptorPool(VulkanOptions options)
    {
        var uniformSets = Math.Max(1u, options.FramesInFlight);
        var textureSets = (uint)Math.Max(1, options.MaxTextures);

        var poolSizes = stackalloc DescriptorPoolSize[]
        {
            new() { Type = DescriptorType.UniformBufferDynamic, DescriptorCount = uniformSets },
            new() { Type = DescriptorType.CombinedImageSampler, DescriptorCount = textureSets }
        };

        DescriptorPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            PoolSizeCount = 2,
            PPoolSizes = poolSizes,
            MaxSets = uniformSets + textureSets
        };

        if (device.Api.CreateDescriptorPool(device.Handle, in poolInfo, null, out var pool) != Result.Success)
        {
            throw new InvalidOperationException("Failed to create the descriptor pool.");
        }

        return pool;
    }

    public DescriptorSet AllocateUniformSet() => AllocateSet(UniformSetLayout);

    public DescriptorSet AllocateTextureSet() => AllocateSet(TextureSetLayout);

    private DescriptorSet AllocateSet(DescriptorSetLayout layout)
    {
        var layouts = stackalloc DescriptorSetLayout[] { layout };

        DescriptorSetAllocateInfo allocInfo = new()
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = DescriptorPool,
            DescriptorSetCount = 1,
            PSetLayouts = layouts
        };

        if (device.Api.AllocateDescriptorSets(device.Handle, in allocInfo, out var set) != Result.Success)
        {
            throw new InvalidOperationException("Failed to allocate a descriptor set — raise VulkanOptions.MaxTextures.");
        }

        return set;
    }

    /// <summary>Points a dynamic uniform set at a buffer; <paramref name="range"/> is one slot.</summary>
    public void UpdateUniformSet(DescriptorSet set, VulkanBuffer buffer, ulong range)
    {
        DescriptorBufferInfo bufferInfo = new()
        {
            Buffer = buffer.Handle,
            Offset = 0,
            Range = range
        };

        WriteDescriptorSet write = new()
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = set,
            DstBinding = 0,
            DstArrayElement = 0,
            DescriptorType = DescriptorType.UniformBufferDynamic,
            DescriptorCount = 1,
            PBufferInfo = &bufferInfo
        };

        device.Api.UpdateDescriptorSets(device.Handle, 1, in write, 0, null);
    }

    public void UpdateTextureSet(DescriptorSet set, VulkanTexture texture)
    {
        DescriptorImageInfo imageInfo = new()
        {
            ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
            ImageView = texture.Image.View,
            Sampler = texture.Sampler
        };

        WriteDescriptorSet write = new()
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = set,
            DstBinding = 0,
            DstArrayElement = 0,
            DescriptorType = DescriptorType.CombinedImageSampler,
            DescriptorCount = 1,
            PImageInfo = &imageInfo
        };

        device.Api.UpdateDescriptorSets(device.Handle, 1, in write, 0, null);
    }

    public void Dispose()
    {
        device.Api.DestroyPipeline(device.Handle, Handle, null);
        device.Api.DestroyPipelineLayout(device.Handle, Layout, null);
        device.Api.DestroyDescriptorPool(device.Handle, DescriptorPool, null);
        device.Api.DestroyDescriptorSetLayout(device.Handle, UniformSetLayout, null);
        device.Api.DestroyDescriptorSetLayout(device.Handle, TextureSetLayout, null);
    }
}
