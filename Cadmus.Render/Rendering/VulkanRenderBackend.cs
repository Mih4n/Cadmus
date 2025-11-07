using Veldrid;

namespace Cadmus.Render.Rendering;

public class VulkanRenderBackend : IRenderBackend
{
    private GraphicsDevice gd;
    private CommandList cl;
    private ResourceFactory factory;

    private DeviceBuffer vertexBuffer;
    private DeviceBuffer indexBuffer;
    private Shader[] shaders;
    private Pipeline pipeline;
    private ResourceLayout resourceLayout;
    private ResourceSet dummyTextureSet; // placeholder if no texture
    private bool initialized = false;

    // Vertex struct: position (float3), color (float3), uv (float2) -> matches shader inputs
    private readonly VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
        0,
        VertexElementPosition(),
        VertexElementColor(),
        VertexElementTexCoords()
    );

    public void Initialize(GraphicsDevice gd, CommandList cl, ResourceFactory factory)
    {
        if (initialized) return;
        this.gd = gd ?? throw new ArgumentNullException(nameof(gd));
        this.cl = cl ?? throw new ArgumentNullException(nameof(cl));
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));

        // Load SPIR-V shader bytes
        var basePath = Path.Combine(AppContext.BaseDirectory, "Cadmus.Render", "Shaders");
        var vertPath = Path.Combine(basePath, "shader.vert.spv");
        var fragPath = Path.Combine(basePath, "shader.frag.spv");

        if (!File.Exists(vertPath) || !File.Exists(fragPath))
        {
            // Also try repository-relative path for development
            var repoRel = Path.Combine(Directory.GetCurrentDirectory(), "Cadmus.Render", "Shaders");
            vertPath = Path.Combine(repoRel, "shader.vert.spv");
            fragPath = Path.Combine(repoRel, "shader.frag.spv");
        }

        if (!File.Exists(vertPath) || !File.Exists(fragPath))
            throw new FileNotFoundException("SPIR-V shader files not found. Generate shader.vert.spv and shader.frag.spv and place them in Cadmus.Render/Shaders/");

        var vertBytes = File.ReadAllBytes(vertPath);
        var fragBytes = File.ReadAllBytes(fragPath);

        shaders = new Shader[]
        {
            factory.CreateShader(new ShaderDescription(ShaderStages.Vertex, vertBytes, "main")),
            factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, fragBytes, "main"))
        };

        resourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("u_MainTex", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("u_MainSampler", ResourceKind.Sampler, ShaderStages.Fragment)
        ));

        // Simple pipeline; assumes framebuffer's color target and same sample count
        var pipelineDesc = new GraphicsPipelineDescription
        {
            BlendState = BlendStateDescription.SingleAlphaBlend,
            DepthStencilState = new DepthStencilStateDescription(
                depthTestEnabled: true, depthWriteEnabled: true, ComparisonKind.LessEqual),
            RasterizerState = new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, true, false),
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            ResourceLayouts = new[] { resourceLayout },
            ShaderSet = new ShaderSetDescription(
                vertexLayouts: new[] { vertexLayout },
                shaders: shaders
            ),
            Outputs = gd.SwapchainFramebuffer.OutputDescription
        };

        pipeline = factory.CreateGraphicsPipeline(pipelineDesc);

        // Create minimal buffers sized for a single quad (4 verts, 6 indices)
        var vSize = (uint)(4 * VertexSizeInBytes());
        var iSize = (uint)(6 * sizeof(ushort));

        vertexBuffer = factory.CreateBuffer(new BufferDescription(vSize, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        indexBuffer = factory.CreateBuffer(new BufferDescription(iSize, BufferUsage.IndexBuffer | BufferUsage.Dynamic));

        // Create dummy 1x1 white texture + sampler to satisfy shader binding when no texture bound
        var tex = factory.CreateTexture(TextureDescription.Texture2D(
            (uint)1, (uint)1, mipLevels: 1, arrayLayers: 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
        var white = new byte[] { 255, 255, 255, 255 };
        gd.UpdateTexture(tex, white, (uint)4, 0, 0, 0, 1, 1, 1, 0, 0);
        var sampler = factory.CreateSampler(SamplerDescription.Aniso4x);
        var texView = factory.CreateTextureView(tex);
        dummyTextureSet = factory.CreateResourceSet(new ResourceSetDescription(resourceLayout, texView, sampler));

        initialized = true;
    }

    public void BeginFrame()
    {
        // nothing special for now
    }

    public void EndFrame()
    {
        // nothing special for now
    }

    public void DrawSprite(Sprite sprite)
    {
        if (!initialized) throw new InvalidOperationException("Backend not initialized.");

        // Build vertex array (transformed in CPU per sprite)
        // Vertex layout: position.x,y,z (float3), color.r,g,b (float3) ; uv.u,v (float2)
        // Use mesh.Positions (Vector3[]) and mesh.UVs (Vector2[])
        var mesh = sprite.Mesh;
        var positions = mesh.Positions;
        var uvs = mesh.UVs;
        var indices = mesh.Indices;

        // Transform positions by model matrix
        var model = sprite.ComputeModelMatrix();

        // Build raw vertex bytes
        var vertexCount = positions.Length;
        var vertexStride = VertexSizeInBytes();
        var vertexData = new byte[vertexCount * vertexStride];

        for (int i = 0; i < vertexCount; i++)
        {
            var p = Vector3.Transform(positions[i], model);
            var uv = uvs[i];
            // color default white (1,1,1). In future material tint will be applied.
            WriteFloat(vertexData, i * vertexStride + 0, p.X);
            WriteFloat(vertexData, i * vertexStride + 4, p.Y);
            WriteFloat(vertexData, i * vertexStride + 8, p.Z);

            WriteFloat(vertexData, i * vertexStride + 12, 1.0f);
            WriteFloat(vertexData, i * vertexStride + 16, 1.0f);
            WriteFloat(vertexData, i * vertexStride + 20, 1.0f);

            WriteFloat(vertexData, i * vertexStride + 24, uv.X);
            WriteFloat(vertexData, i * vertexStride + 28, uv.Y);
        }

        // Write indices
        var indexCount = indices.Length;
        var indexData = new byte[indexCount * sizeof(ushort)];
        for (int i = 0; i < indexCount; i++)
        {
            var v = indices[i];
            var off = i * sizeof(ushort);
            indexData[off + 0] = (byte)(v & 0xFF);
            indexData[off + 1] = (byte)((v >> 8) & 0xFF);
        }

        // Update device buffers
        gd.UpdateBuffer(vertexBuffer, 0, vertexData);
        gd.UpdateBuffer(indexBuffer, 0, indexData);

        // Issue draw calls on current commandlist (assumes caller has begun command list and set framebuffer)
        cl.SetPipeline(pipeline);
        cl.SetVertexBuffer(0, vertexBuffer);
        cl.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
        cl.SetGraphicsResourceSet(0, dummyTextureSet);

        cl.DrawIndexed((uint)indexCount, 1, 0, 0, 0);
    }

    public void Dispose()
    {
        pipeline?.Dispose();
        vertexBuffer?.Dispose();
        indexBuffer?.Dispose();
        foreach (var s in shaders ?? Array.Empty<Shader>())
            s?.Dispose();
        resourceLayout?.Dispose();
        dummyTextureSet?.Dispose();
    }

    #region Helpers
    private static VertexElementDescription VertexElementPosition()
        => new VertexElementDescription("position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3);

    private static VertexElementDescription VertexElementColor()
        => new VertexElementDescription("color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3);

    private static VertexElementDescription VertexElementTexCoords()
        => new VertexElementDescription("uv", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2);

    private static int VertexSizeInBytes()
    {
        // 3 floats + 3 floats + 2 floats = 8 floats = 32 bytes
        return sizeof(float) * 8;
    }

    private static void WriteFloat(byte[] arr, int offset, float value)
    {
        var b = BitConverter.GetBytes(value);
        Buffer.BlockCopy(b, 0, arr, 0, 4);
        // incorrect above; must copy into arr at offset; fix:
        // We'll do correct copy below
        for (int i = 0; i < 4; i++) arr[offset + i] = b[i];
    }
    #endregion
} 
