using System.Numerics;
using Veldrid;

namespace Cadmus.Render.Rendering;

public class VulkanRenderBackend : IRenderBackend
{
    private GraphicsDevice gd;
    private CommandList cl;
    private ResourceFactory factory;

    private DeviceBuffer vertexBuffer;
    private DeviceBuffer indexBuffer;
    private Veldrid.Shader[] shaders;
    private Pipeline pipeline;
    private ResourceLayout resourceLayout;
    private ResourceSet dummyTextureSet; 
    private bool initialized = false;

    private readonly VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
        8,
        VertexElementPosition(),
        VertexElementColor(),
        VertexElementTexCoords()
    );

    public VulkanRenderBackend(GraphicsDevice gd, CommandList cl)
    {
        this.gd = gd;
        this.cl = cl;
        factory = gd.ResourceFactory;

        var basePath = Path.Combine(AppContext.BaseDirectory, "Shaders");
        var vertPath = Path.Combine(basePath, "sprite.vert.spv");
        var fragPath = Path.Combine(basePath, "sprite.frag.spv");

        if (!File.Exists(vertPath) || !File.Exists(fragPath))
            throw new FileNotFoundException("SPIR-V shader files not found. Generate shader.vert.spv and shader.frag.spv and place them in Cadmus.Render/Shaders/");

        var vertBytes = File.ReadAllBytes(vertPath);
        var fragBytes = File.ReadAllBytes(fragPath);

        shaders = [
            factory.CreateShader(new ShaderDescription(ShaderStages.Vertex, vertBytes, "main")),
            factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, fragBytes, "main"))
        ];

        resourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("u_MainTex", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("u_MainSampler", ResourceKind.Sampler, ShaderStages.Fragment)
        ));

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

        var vSize = (uint)(4 * VertexSizeInBytes());
        var iSize = (uint)(6 * sizeof(ushort));

        vertexBuffer = factory.CreateBuffer(new BufferDescription(vSize, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        indexBuffer = factory.CreateBuffer(new BufferDescription(iSize, BufferUsage.IndexBuffer | BufferUsage.Dynamic));

        var tex = factory.CreateTexture(TextureDescription.Texture2D(1, 1, mipLevels: 1, arrayLayers: 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
        var white = new byte[] { 255, 255, 255, 255 };
        gd.UpdateTexture(tex, white, 0, 0, 0, 1, 1, 1, 0, 0);
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

        var mesh = sprite.Mesh;
        var positions = mesh.Positions;
        var uvs = mesh.UVs;
        var indices = mesh.Indices;

        var model = sprite.ComputeModelMatrix();

        var vertexCount = positions.Length;
        var vertexStride = VertexSizeInBytes();
        var vertexData = new byte[vertexCount * vertexStride];

        for (int i = 0; i < vertexCount; i++)
        {
            var p = Vector3.Transform(positions[i], model);
            var uv = uvs[i];
            WriteFloat(vertexData, i * vertexStride + 0, p.X);
            WriteFloat(vertexData, i * vertexStride + 4, p.Y);
            WriteFloat(vertexData, i * vertexStride + 8, p.Z);

            WriteFloat(vertexData, i * vertexStride + 12, 1.0f);
            WriteFloat(vertexData, i * vertexStride + 16, 1.0f);
            WriteFloat(vertexData, i * vertexStride + 20, 1.0f);

            WriteFloat(vertexData, i * vertexStride + 24, uv.X);
            WriteFloat(vertexData, i * vertexStride + 28, uv.Y);
        }

        var indexCount = indices.Length;
        var indexData = new byte[indexCount * sizeof(ushort)];
        for (int i = 0; i < indexCount; i++)
        {
            var v = indices[i];
            var off = i * sizeof(ushort);
            indexData[off + 0] = (byte)(v & 0xFF);
            indexData[off + 1] = (byte)((v >> 8) & 0xFF);
        }

        gd.UpdateBuffer(vertexBuffer, 0, vertexData);
        gd.UpdateBuffer(indexBuffer, 0, indexData);

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
        foreach (var s in shaders)
            s?.Dispose();
        resourceLayout?.Dispose();
        dummyTextureSet?.Dispose();
    }

    private static VertexElementDescription VertexElementPosition()
        => new VertexElementDescription("position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3);

    private static VertexElementDescription VertexElementColor()
        => new VertexElementDescription("color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3);

    private static VertexElementDescription VertexElementTexCoords()
        => new VertexElementDescription("uv", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2);

    private static int VertexSizeInBytes()
    {
        return sizeof(float) * 8;
    }

    private static void WriteFloat(byte[] arr, int offset, float value)
    {
        var b = BitConverter.GetBytes(value);
        Buffer.BlockCopy(b, 0, arr, 0, 4);

        for (int i = 0; i < 4; i++) arr[offset + i] = b[i];
    }
} 
