using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Cadmus.Render;

public unsafe class VulkanPipeline : IDisposable
{
    private readonly Vk _vk;
    private readonly VulkanDevice _device;
    private readonly VulkanRenderPass _renderPass;
    private readonly Extent2D _extent;

    public DescriptorSetLayout[] SetLayouts { get; private set; } = [];
    public PipelineLayout PipelineLayout { get; private set; }
    public Pipeline GraphicsPipeline { get; private set; }
    public DescriptorPool DescriptorPool { get; private set; }

    public VulkanPipeline(Vk vk, VulkanDevice device, VulkanRenderPass renderPass, Extent2D extent, string vertShaderPath, string fragShaderPath)
    {
        _vk = vk;
        _device = device;
        _renderPass = renderPass;
        _extent = extent;

        CreateDescriptorSetLayouts();
        CreateGraphicsPipeline(vertShaderPath, fragShaderPath);
        CreateDescriptorPool();
    }

    private void CreateDescriptorSetLayouts()
    {
        // Set 0: Uniform buffer (Matrices)
        DescriptorSetLayoutBinding uboLayoutBinding = new()
        {
            Binding = 0,
            DescriptorType = DescriptorType.UniformBuffer,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.VertexBit
        };

        DescriptorSetLayoutCreateInfo uboLayoutInfo = new()
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = 1,
            PBindings = &uboLayoutBinding
        };

        if (_vk.CreateDescriptorSetLayout(_device.Device, in uboLayoutInfo, null, out DescriptorSetLayout uboLayout) != Result.Success)
        {
            throw new Exception("Failed to create UBO descriptor set layout!");
        }

        // Set 1: Combined image sampler (Texture)
        DescriptorSetLayoutBinding samplerLayoutBinding = new()
        {
            Binding = 0,
            DescriptorType = DescriptorType.CombinedImageSampler,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.FragmentBit
        };

        DescriptorSetLayoutCreateInfo samplerLayoutInfo = new()
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = 1,
            PBindings = &samplerLayoutBinding
        };

        if (_vk.CreateDescriptorSetLayout(_device.Device, in samplerLayoutInfo, null, out DescriptorSetLayout samplerLayout) != Result.Success)
        {
            throw new Exception("Failed to create sampler descriptor set layout!");
        }

        SetLayouts = new[] { uboLayout, samplerLayout };
    }

    private void CreateGraphicsPipeline(string vertPath, string fragPath)
    {
        using var vertShader = VulkanShaderModule.FromFile(_vk, _device.Device, vertPath);
        using var fragShader = VulkanShaderModule.FromFile(_vk, _device.Device, fragPath);

        PipelineShaderStageCreateInfo vertShaderStageInfo = new()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.VertexBit,
            Module = vertShader.Module,
            PName = (byte*)SilkMarshal.StringToPtr("main")
        };

        PipelineShaderStageCreateInfo fragShaderStageInfo = new()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.FragmentBit,
            Module = fragShader.Module,
            PName = (byte*)SilkMarshal.StringToPtr("main")
        };

        var shaderStages = stackalloc PipelineShaderStageCreateInfo[] { vertShaderStageInfo, fragShaderStageInfo };

        var bindingDescription = Vertex.GetBindingDescription();
        var attributeDescriptions = Vertex.GetAttributeDescriptions();

        fixed (VertexInputAttributeDescription* pAttributeDescriptions = attributeDescriptions)
        {
            PipelineVertexInputStateCreateInfo vertexInputInfo = new()
            {
                SType = StructureType.PipelineVertexInputStateCreateInfo,
                VertexBindingDescriptionCount = 1,
                PVertexBindingDescriptions = &bindingDescription,
                VertexAttributeDescriptionCount = (uint)attributeDescriptions.Length,
                PVertexAttributeDescriptions = pAttributeDescriptions
            };

            PipelineInputAssemblyStateCreateInfo inputAssembly = new()
            {
                SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                Topology = PrimitiveTopology.TriangleList,
                PrimitiveRestartEnable = false
            };

            Viewport viewport = new()
            {
                X = 0,
                Y = 0,
                Width = _extent.Width,
                Height = _extent.Height,
                MinDepth = 0,
                MaxDepth = 1
            };

            Rect2D scissor = new()
            {
                Offset = new Offset2D { X = 0, Y = 0 },
                Extent = _extent
            };

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
                CullMode = CullModeFlags.BackBit,
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
                DepthCompareOp = CompareOp.Less,
                DepthBoundsTestEnable = false,
                StencilTestEnable = false
            };

            PipelineColorBlendAttachmentState colorBlendAttachment = new()
            {
                ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
                BlendEnable = true,
                SrcColorBlendFactor = BlendFactor.SrcAlpha,
                DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
                ColorBlendOp = BlendOp.Add,
                SrcAlphaBlendFactor = BlendFactor.One,
                DstAlphaBlendFactor = BlendFactor.Zero,
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

            fixed (DescriptorSetLayout* pSetLayouts = SetLayouts)
            {
                PipelineLayoutCreateInfo pipelineLayoutInfo = new()
                {
                    SType = StructureType.PipelineLayoutCreateInfo,
                    SetLayoutCount = (uint)SetLayouts.Length,
                    PSetLayouts = pSetLayouts
                };

                if (_vk.CreatePipelineLayout(_device.Device, in pipelineLayoutInfo, null, out PipelineLayout pipelineLayout) != Result.Success)
                {
                    throw new Exception("Failed to create pipeline layout!");
                }
                PipelineLayout = pipelineLayout;
            }

            GraphicsPipelineCreateInfo pipelineInfo = new()
            {
                SType = StructureType.GraphicsPipelineCreateInfo,
                StageCount = 2,
                PStages = shaderStages,
                PVertexInputState = &vertexInputInfo,
                PInputAssemblyState = &inputAssembly,
                PViewportState = &viewportState,
                PRasterizationState = &rasterizer,
                PMultisampleState = &multisampling,
                PDepthStencilState = &depthStencil,
                PColorBlendState = &colorBlending,
                PDynamicState = &dynamicState,
                Layout = PipelineLayout,
                RenderPass = _renderPass.RenderPass,
                Subpass = 0,
                BasePipelineHandle = default
            };

            if (_vk.CreateGraphicsPipelines(_device.Device, default, 1, in pipelineInfo, null, out Pipeline pipeline) != Result.Success)
            {
                throw new Exception("Failed to create graphics pipeline!");
            }
            GraphicsPipeline = pipeline;

            SilkMarshal.Free((nint)vertShaderStageInfo.PName);
            SilkMarshal.Free((nint)fragShaderStageInfo.PName);
        }
    }

    private void CreateDescriptorPool()
    {
        var poolSizes = stackalloc DescriptorPoolSize[]
        {
            new DescriptorPoolSize { Type = DescriptorType.UniformBuffer, DescriptorCount = 1 },
            new DescriptorPoolSize { Type = DescriptorType.CombinedImageSampler, DescriptorCount = 1 }
        };

        DescriptorPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            PoolSizeCount = 2,
            PPoolSizes = poolSizes,
            MaxSets = 2
        };

        if (_vk.CreateDescriptorPool(_device.Device, in poolInfo, null, out DescriptorPool pool) != Result.Success)
        {
            throw new Exception("Failed to create descriptor pool!");
        }
        DescriptorPool = pool;
    }

    public DescriptorSet[] AllocateDescriptorSets()
    {
        var layouts = stackalloc DescriptorSetLayout[SetLayouts.Length];
        for (int i = 0; i < SetLayouts.Length; i++) layouts[i] = SetLayouts[i];

        DescriptorSetAllocateInfo allocInfo = new()
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = DescriptorPool,
            DescriptorSetCount = (uint)SetLayouts.Length,
            PSetLayouts = layouts
        };

        var sets = new DescriptorSet[SetLayouts.Length];
        fixed (DescriptorSet* pSets = sets)
        {
            if (_vk.AllocateDescriptorSets(_device.Device, in allocInfo, pSets) != Result.Success)
            {
                throw new Exception("Failed to allocate descriptor sets!");
            }
        }

        return sets;
    }

    public void UpdateDescriptorSets(DescriptorSet[] sets, VulkanUniformBuffer ubo, VulkanTexture texture)
    {
        DescriptorBufferInfo bufferInfo = new()
        {
            Buffer = ubo.Buffer.Buffer,
            Offset = 0,
            Range = ubo.Size
        };

        DescriptorImageInfo imageInfo = new()
        {
            ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
            ImageView = texture.VulkanImage.ImageView,
            Sampler = texture.Sampler
        };

        var writeUbo = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = sets[0],
            DstBinding = 0,
            DstArrayElement = 0,
            DescriptorType = DescriptorType.UniformBuffer,
            DescriptorCount = 1,
            PBufferInfo = &bufferInfo
        };

        var writeSampler = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = sets[1],
            DstBinding = 0,
            DstArrayElement = 0,
            DescriptorType = DescriptorType.CombinedImageSampler,
            DescriptorCount = 1,
            PImageInfo = &imageInfo
        };

        var writes = stackalloc WriteDescriptorSet[] { writeUbo, writeSampler };
        _vk.UpdateDescriptorSets(_device.Device, 2, writes, 0, null);
    }

    public void Dispose()
    {
        _vk.DestroyPipeline(_device.Device, GraphicsPipeline, null);
        _vk.DestroyPipelineLayout(_device.Device, PipelineLayout, null);
        foreach (var layout in SetLayouts)
        {
            _vk.DestroyDescriptorSetLayout(_device.Device, layout, null);
        }
        _vk.DestroyDescriptorPool(_device.Device, DescriptorPool, null);
    }
}
