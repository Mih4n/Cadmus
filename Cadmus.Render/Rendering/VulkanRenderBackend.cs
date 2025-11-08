using System.Numerics;
using System.Runtime.InteropServices;
using Cadmus.Domain.Components.Rendering;
using Cadmus.Domain.Components.Sprites;
using Cadmus.Domain.Contracts.Game;
using Veldrid;

namespace Cadmus.Render.Rendering;

public class VulkanRenderBackend : IRenderBackend
{
    public VulkanRenderingContextComponent context;

    private DeviceBuffer vertexBuffer;
    private DeviceBuffer indexBuffer;

    private DeviceBuffer matrixBuffer;

    private Pipeline pipeline;
    private ResourceLayout matrixLayout; 
    private ResourceLayout textureLayout;

    private ResourceSet matrixSet;
    private ResourceSet dummyTextureSet;

    private Sampler sampler;
    private Dictionary<Texture, ResourceSet> textureSets;

    private readonly VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
        new VertexElementDescription("position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
        new VertexElementDescription("uv", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2)
    );
    
    private static readonly float[] QuadVertices =
    {
        -0.5f, -0.5f, 0f, 0f, 1f,
         0.5f, -0.5f, 0f, 1f, 1f,
         0.5f,  0.5f, 0f, 1f, 0f,
        -0.5f,  0.5f, 0f, 0f, 0f
    };
    private static readonly ushort[] QuadIndices = { 0, 1, 2, 0, 2, 3 };

    private const uint MatrixBufferSize = 128; // 2 * mat4

    public VulkanRenderBackend(IGameContext gameContext)
    {
        context = gameContext
            .Game
            .TryGetComponent<VulkanRenderingContextComponent>(out var component) ? 
                component : 
                throw new Exception("no component");

        textureSets = new Dictionary<Texture, ResourceSet>();

        var device = context.Device;
        var factory = device.ResourceFactory;

        vertexBuffer = factory.CreateBuffer(new BufferDescription((uint)(QuadVertices.Length * sizeof(float)), BufferUsage.VertexBuffer));
        indexBuffer = factory.CreateBuffer(new BufferDescription((uint)(QuadIndices.Length * sizeof(ushort)), BufferUsage.IndexBuffer));

        device.UpdateBuffer(vertexBuffer, 0, QuadVertices);
        device.UpdateBuffer(indexBuffer, 0, QuadIndices);

        var basePath = Path.Combine(AppContext.BaseDirectory, "Assets/Shaders");
        var vertPath = Path.Combine(basePath, "sprite.vert.spv");
        var fragPath = Path.Combine(basePath, "sprite.frag.spv");

        if (!File.Exists(vertPath) || !File.Exists(fragPath))
            throw new FileNotFoundException("SPIR-V shader files not found.");
        
        var vertBytes = File.ReadAllBytes(vertPath);
        var fragBytes = File.ReadAllBytes(fragPath);

        var shaders = new[]
        {
            factory.CreateShader(new ShaderDescription(ShaderStages.Vertex, vertBytes, "main")),
            factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, fragBytes, "main"))
        };

        matrixLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("Matrices", ResourceKind.UniformBuffer, ShaderStages.Vertex)
        ));

        textureLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("u_MainTex", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("u_MainSampler", ResourceKind.Sampler, ShaderStages.Fragment)
        ));

        var pipelineDesc = new GraphicsPipelineDescription
        {
            BlendState = BlendStateDescription.SingleAlphaBlend,
            DepthStencilState = new DepthStencilStateDescription(
                depthTestEnabled: false,
                depthWriteEnabled: false,
                comparisonKind: ComparisonKind.LessEqual),
            RasterizerState = new RasterizerStateDescription(
                cullMode: FaceCullMode.None,
                fillMode: PolygonFillMode.Solid,
                frontFace: FrontFace.Clockwise,
                depthClipEnabled: true,
                scissorTestEnabled: false),
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            ResourceLayouts = [matrixLayout, textureLayout], 
            ShaderSet = new ShaderSetDescription([vertexLayout], shaders),
            Outputs = device.SwapchainFramebuffer.OutputDescription
        };
        pipeline = factory.CreateGraphicsPipeline(pipelineDesc);

        matrixBuffer = factory.CreateBuffer(new BufferDescription(MatrixBufferSize, BufferUsage.UniformBuffer | BufferUsage.Dynamic));

        matrixSet = factory.CreateResourceSet(new ResourceSetDescription(matrixLayout, matrixBuffer));

        sampler = factory.CreateSampler(SamplerDescription.Aniso4x);

        byte[] white = { 255, 255, 255, 255 };
        var tex = factory.CreateTexture(TextureDescription.Texture2D(1, 1, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
        device.UpdateTexture(tex, white, 0, 0, 0, 1, 1, 1, 0, 0);
        
        var texView = factory.CreateTextureView(tex);
        
        dummyTextureSet = factory.CreateResourceSet(new ResourceSetDescription(textureLayout, texView, sampler));
    }

    private ResourceSet GetOrCreateTextureSet(Texture texture)
    {
        if (textureSets.TryGetValue(texture, out var set))
        {
            return set;
        }

        var factory = context.Device.ResourceFactory;

        var texView = factory.CreateTextureView(texture);
        var newSet = factory.CreateResourceSet(new ResourceSetDescription(textureLayout, texView, sampler));
        
        textureSets.Add(texture, newSet);
        return newSet;
    }

    public void BeginFrame() { }
    public void EndFrame() { }

    public void DrawSprite(SpriteComponent sprite, Matrix4x4 viewProjection)
    {
        var model = sprite.ComputeModelMatrix();
        var device = context.Device;
        var commands = context.Commands;

        MappedResource map = device.Map(matrixBuffer, MapMode.Write);
        try
        {
            Marshal.Copy(MemoryMarshal.AsBytes(new ReadOnlySpan<Matrix4x4>(ref viewProjection)).ToArray(), 0, map.Data, 64);
            Marshal.Copy(MemoryMarshal.AsBytes(new ReadOnlySpan<Matrix4x4>(ref model)).ToArray(), 0, map.Data + 64, 64);
        }
        finally
        {
            device.Unmap(matrixBuffer);
        }

        commands.SetPipeline(pipeline);
        commands.SetVertexBuffer(0, vertexBuffer);
        commands.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
        
        commands.SetGraphicsResourceSet(0, matrixSet);

        var textureSet = (sprite.Texture != null)
            ? GetOrCreateTextureSet(sprite.Texture)
            : dummyTextureSet;

        commands.SetGraphicsResourceSet(1, textureSet); 
        commands.DrawIndexed(indexCount: (uint)QuadIndices.Length, 1, 0, 0, 0);
    }


    public void Dispose()
    {
        pipeline.Dispose();
        matrixLayout.Dispose();
        textureLayout.Dispose();
        matrixBuffer.Dispose();
        matrixSet.Dispose();
        
        sampler.Dispose();
        foreach (var set in textureSets.Values)
        {
            set.Dispose(); 
        }
        dummyTextureSet.Dispose();
        
        vertexBuffer.Dispose();
        indexBuffer.Dispose();
    }
}