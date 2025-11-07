using Cadmus.Domain.Contracts.Rendering; // <-- Убедитесь, что у вас есть IShader
using Cadmus.Render.Camera;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace Cadmus.Render.Rendering;

public class VulkanRenderBackend : IRenderBackend
{
    private GraphicsDevice gd;
    private CommandList cl;
    private ResourceFactory factory;

    // Буферы для одного квадрата (unit quad)
    private DeviceBuffer vertexBuffer;
    private DeviceBuffer indexBuffer;

    // Буфер для матриц (ViewProjection и Model)
    private DeviceBuffer matrixBuffer;

    private Pipeline pipeline;
    private ResourceLayout matrixLayout; // Set 0
    private ResourceLayout textureLayout; // Set 1

    private ResourceSet matrixSet; // Set 0
    private ResourceSet dummyTextureSet; // Set 1 (Фоллбэк для белого квадрата)

    // --- НОВЫЕ ПОЛЯ ---
    private Sampler sampler; // Сэмплер, используемый для ВСЕХ текстур
    private Dictionary<Texture, ResourceSet> textureSets; // Кэш для ResourceSet'ов

    // Определение вершин (позиция + UV)
    private readonly VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
        new VertexElementDescription("position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
        new VertexElementDescription("uv", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2)
    );
    
    // Данные для квадрата
    private static readonly float[] QuadVertices =
    {
        // X, Y, Z, U, V
        -0.5f, -0.5f, 0f, 0f, 1f,
         0.5f, -0.5f, 0f, 1f, 1f,
         0.5f,  0.5f, 0f, 1f, 0f,
        -0.5f,  0.5f, 0f, 0f, 0f
    };
    private static readonly ushort[] QuadIndices = { 0, 1, 2, 0, 2, 3 };

    // Размер структуры Uniform-буфера (mat4 = 64 байта)
    private const uint MatrixBufferSize = 128; // 2 * mat4


    public VulkanRenderBackend(GraphicsDevice gd, CommandList cl)
    {
        this.gd = gd;
        this.cl = cl;
        this.factory = gd.ResourceFactory;

        // --- ИНИЦИАЛИЗАЦИЯ КЭША ---
        this.textureSets = new Dictionary<Texture, ResourceSet>();

        // --- 1. Создание буферов для меша (один раз) ---
        vertexBuffer = factory.CreateBuffer(new BufferDescription((uint)(QuadVertices.Length * sizeof(float)), BufferUsage.VertexBuffer));
        indexBuffer = factory.CreateBuffer(new BufferDescription((uint)(QuadIndices.Length * sizeof(ushort)), BufferUsage.IndexBuffer));

        // Загрузка данных в буферы
        gd.UpdateBuffer(vertexBuffer, 0, QuadVertices);
        gd.UpdateBuffer(indexBuffer, 0, QuadIndices);

        // --- 2. Загрузка шейдеров ---
        var basePath = Path.Combine(AppContext.BaseDirectory, "Shaders");
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

        // --- 3. Создание Resource Layouts ---

        // Set 0: Матрицы (VS)
        matrixLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("Matrices", ResourceKind.UniformBuffer, ShaderStages.Vertex)
        ));

        // Set 1: Текстура и Сэмплер (FS)
        textureLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("u_MainTex", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("u_MainSampler", ResourceKind.Sampler, ShaderStages.Fragment)
        ));

        // --- 4. Создание пайплайна ---
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
            ResourceLayouts = new[] { matrixLayout, textureLayout }, // ОБА ЛАЙАУТА
            ShaderSet = new ShaderSetDescription(new[] { vertexLayout }, shaders),
            Outputs = gd.SwapchainFramebuffer.OutputDescription
        };
        pipeline = factory.CreateGraphicsPipeline(pipelineDesc);

        // --- 5. Создание ресурсов ---

        // Буфер для матриц
        matrixBuffer = factory.CreateBuffer(new BufferDescription(MatrixBufferSize, BufferUsage.UniformBuffer | BufferUsage.Dynamic));

        // ResourceSet для матриц (Set 0)
        matrixSet = factory.CreateResourceSet(new ResourceSetDescription(matrixLayout, matrixBuffer));

        // --- ИЗМЕНЕНО: Создание Dummy Текстуры и Сэмплера ---
        
        // Создаем сэмплер ОДИН РАЗ и сохраняем его
        sampler = factory.CreateSampler(SamplerDescription.Aniso4x);

        // Белый пиксель для "dummy" текстуры
        byte[] white = { 255, 255, 255, 255 };
        var tex = factory.CreateTexture(TextureDescription.Texture2D(1, 1, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
        gd.UpdateTexture(tex, white, 0, 0, 0, 1, 1, 1, 0, 0);
        
        var texView = factory.CreateTextureView(tex);
        
        // ResourceSet для dummy-текстуры (Set 1)
        dummyTextureSet = factory.CreateResourceSet(new ResourceSetDescription(textureLayout, texView, sampler));
    }

    // --- НОВЫЙ МЕТОД: Получает или создает ResourceSet для текстуры ---
    private ResourceSet GetOrCreateTextureSet(Texture texture)
    {
        if (textureSets.TryGetValue(texture, out var set))
        {
            return set;
        }

        // Если текстура не найдена в кэше, создаем для нее новый ResourceSet
        var texView = factory.CreateTextureView(texture);
        var newSet = factory.CreateResourceSet(new ResourceSetDescription(textureLayout, texView, sampler));
        
        // Кэшируем его
        textureSets.Add(texture, newSet);
        return newSet;
    }

    public void BeginFrame() { }
    public void EndFrame() { }

    // ИЗМЕНЕНО: Метод теперь принимает ViewProjection матрицу
    public void DrawSprite(Sprite sprite, Matrix4x4 viewProjection)
    {
        // 1. Получаем матрицы
        var model = sprite.ComputeModelMatrix();

        // 2. Обновляем Uniform Buffer (CPU -> GPU)
        MappedResource map = gd.Map(matrixBuffer, MapMode.Write);
        try
        {
            // Копируем ViewProjection (Offset 0)
            Marshal.Copy(MemoryMarshal.AsBytes(new ReadOnlySpan<Matrix4x4>(ref viewProjection)).ToArray(), 0, map.Data, 64);
            // Копируем Model (Offset 64)
            Marshal.Copy(MemoryMarshal.AsBytes(new ReadOnlySpan<Matrix4x4>(ref model)).ToArray(), 0, map.Data + 64, 64);
        }
        finally
        {
            gd.Unmap(matrixBuffer);
        }

        // 3. Устанавливаем пайплайн и ресурсы
        cl.SetPipeline(pipeline);
        cl.SetVertexBuffer(0, vertexBuffer);
        cl.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
        
        // 4. Привязываем ResourceSet 0 (Матрицы)
        cl.SetGraphicsResourceSet(0, matrixSet);

        // --- ИЗМЕНЕНО: Привязываем ПРАВИЛЬНЫЙ ResourceSet 1 (Текстура) ---

        // 4b. Получаем текстуру из материала спрайта
        var texture = (sprite.Material.Textures.Length > 0) 
            ? sprite.Material.Textures[0] // Берем первую текстуру
            : null;

        // 4c. Получаем ResourceSet для этой текстуры (или фоллбэк, если текстуры нет)
        var textureSet = (texture != null)
            ? GetOrCreateTextureSet(texture)
            : dummyTextureSet; // Используем белый квадрат, если у материала нет текстур

        // 4d. Привязываем его
        cl.SetGraphicsResourceSet(1, textureSet); 

        // 5. Рисуем 6 индексов (1 квадрат)
        cl.DrawIndexed(indexCount: (uint)QuadIndices.Length, 1, 0, 0, 0);
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