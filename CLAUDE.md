# Cadmus

Modular ECS-style game engine on **.NET 10 / C# 14**, rendering through **Silk.NET + Vulkan**.
Everything is wired with **Microsoft.Extensions.DependencyInjection** — there is no ambient context
and no service locator anywhere in the engine.

## Build & run

The environment is [devenv](https://devenv.sh) (`devenv.nix`). **Everything must run inside it** —
GLFW dlopens X11/Wayland at runtime and the copy Silk.NET bundles has no RPATH, so outside the shell
window creation dies with *"Couldn't find a suitable window platform (GlfwPlatform - not
applicable)"*. `direnv allow` once makes entering the directory enough.

```bash
devenv shell            # or: direnv allow, once

build                   # dotnet build Cadmus.sln
run                     # build + launch TestGame in a window
screenshot [path]       # renders ~95 frames, writes a PNG, exits (no visible session needed)
shaders                 # recompile Assets/Shaders/*.{vert,frag} to .spv with glslc
doctor                  # check dotnet, glslc, Vulkan device, validation layers, display
```

Build output goes to `.Output/<ProjectName>/` (set by `Directory.Build.props`), **not** `bin/`.
Package versions are centrally managed in `Directory.Packages.props`; a `PackageReference` must not
carry a `Version` attribute.

## Project layout

| Project | Contains | May reference |
| --- | --- | --- |
| `Cadmus.Core` | Interfaces and primitives only. **No Silk.NET, no Vulkan, no DI container.** | nothing |
| `Cadmus.Engine` | Components, entities, scenes, geometry, DI registration helpers | Core |
| `Cadmus.Graphics` | Window + every Vulkan object, GPU resource cache, frame capture | Core, Engine |
| `Cadmus.Rendering` | Per-frame render systems (`ResourceUploadSystem`, `VulkanRenderSystem`) | Core, Engine, Graphics |
| `Cadmus.App` | `CadmusApplication` builder, `GameHost` frame loop, `Game` base class | all of the above |
| `TestGame` | Runnable demo | Engine, App |

Layer names follow the vocabulary engines actually use (Stride, Unreal), not DDD: **Core** is what
everything depends on, **Engine** is the runtime model (ECS + scenes), **Graphics** is the low-level
GPU/window backend, **Rendering** is what walks a scene and produces draw calls.

Registration helper per layer: `AddCadmusEngine()`, `AddCadmusGraphics()`, `AddCadmusRendering()` —
all called for you by `CadmusApplication.CreateBuilder()`.

Keep `Cadmus.Core` backend-agnostic. `IGameWindow` deliberately exposes
`(int Width, int Height)` instead of `Vector2D<int>` so contracts never see a Silk type.

## Architecture rules

**Dependencies come from the constructor, always.** Systems, scenes, entities and the game itself
are resolved from the container. A system that needs the active scene injects `ISceneManager` and
reads `Current`; it is never handed a context object.

```csharp
public sealed class MyScene(IEntityFactory entities, IGameWindow window) : Scene(entities) { }
```

- Entities are created via `IEntityFactory` (`Scene.Spawn<T>()`), which uses
  `ActivatorUtilities`, so entity constructors can take services — `SnakeEntity` takes
  `IInputService` this way.
- Scenes are registered by name — `services.AddScene<MainScene>("Main")` — and loaded through
  `ISceneManager.LoadAsync("Main")`. Each loaded scene gets its own service scope.
- Systems are registered with `services.AddSystem<T>()` and run in `ISystem.Order` order,
  sequentially (they share scene state). `IRenderSystem.Render` runs after all updates.
- The container is built with `ValidateOnBuild` — a circular dependency fails at startup, not at
  frame 1. `IGame` must therefore never depend on `IGameHost`; stop the loop with
  `IGameWindow.Close()`.
- Vulkan objects are singletons in the container. Creation order and reverse-order disposal are the
  container's job — do not add manual init/teardown routines.
- Keyboard state is `IInputService` (Core) implemented by `SilkInputService` (Graphics), which is
  itself a system at `int.MinValue` so it refreshes before any gameplay code runs. It polls rather
  than subscribing: the host pumps the window immediately before, so a poll cannot miss an edge.
- `IFrameStatistics` (Core) carries fps, frame-time min/max, draw calls, entity and GPU-cache counts.
  `FrameStatistics` samples timing as a system; the render layer fills in what only it knows.
  `DebugOverlay` draws it as a screen-space HUD, toggled with **F3**.

## Rendering notes

Facts that are easy to break and expensive to rediscover:

- **Shaders are pre-compiled.** `Assets/Shaders/*.spv` are committed; run `shaders` after editing the
  GLSL. Prefer changes that do not require recompiling them.
- **Per-object matrices use a dynamic uniform buffer** (`UniformRing`), one slot per draw. GLSL
  declares `UNIFORM_BUFFER` and `UNIFORM_BUFFER_DYNAMIC` identically, so this needed no shader
  change. Writing a single shared uniform per draw would make every draw use the last matrix.
- **Matrix upload is transposed on purpose.** System.Numerics is row-vector/row-major; its raw bytes
  read back in GLSL as the transpose, which is exactly what `u_ViewProj * u_Model * v` wants. Upload
  `view * projection` in .NET order and do not transpose by hand.
- **Pixel-space camera is Y-down** (`CreateOrthographicOffCenter(0, w, 0, h, ...)` under Vulkan's
  Y-down NDC), so the unit quad's UVs also run V-downwards. Changing one without the other flips
  every sprite.
- Cull mode is `None`: 2D quads flip winding with the projection's handedness.
- **Tints are linear, not sRGB.** The swapchain is an sRGB format and converts on write, so a value
  authored by eye looks washed out. Build tints with `Colors.FromHex` / `Colors.FromSrgb`, which do
  the conversion. `VulkanOptions.ClearColor` is linear too.
- The uniform block is visible to **both** shader stages: the vertex stage reads the matrices, the
  fragment stage the tint. Both `sprite.vert` and `sprite.frag` must declare it identically.
- Larger `z` draws in front; the collector also sorts by depth for correct alpha blending.
- Textures are cached by path in `IGpuResourceCache`, one descriptor set per texture. Missing files
  fall back to `Assets/Textures/fallback.png` instead of throwing.
- **Meshes are cached by reference.** Reuse `Mesh.UnitQuad`; calling `Mesh.CreateUnitQuad()` per
  sprite per frame uploads a new vertex buffer every time and leaks GPU memory — the HUD's `meshes`
  counter is how that was caught. Only build a new `Mesh` for genuinely new geometry (glyphs, atlas
  sub-rects via `Mesh.CreateQuad`).
- `RenderItem.ScreenSpace` swaps the scene camera for a pixel-space projection of the framebuffer,
  so the HUD is unaffected by a scrolling or zoomed game camera.
- Text is one textured quad per glyph from `Assets/Textures/font.png` (ASCII 32-126, 16x24 cells,
  16 per row), driven by `BitmapFont`.
- Validation layers are enabled in DEBUG and the devenv provides them via `VK_LAYER_PATH`. Treat any
  `[Vulkan:ErrorBitExt]` line as a bug — they caught two real spec violations in the capture path.
- Swapchain images request `TransferSrcBit` so a presented frame can be copied out, and capture must
  happen **before** `QueuePresent`, while the image is still acquired.

## Code style

Multi-line calls put **one argument per line** with the **closing parenthesis on its own line**,
including nested constructors. Short calls stay on one line.

```csharp
Spawn(
    "Companions",
    new PositionComponent(center.X, center.Y, 1),
    new SpriteComponent(
        "Assets/Textures/fallback.png",
        new Vector2(48, 48),
        new PositionComponent(-140, -90, 0)
    )
);
```

Other conventions in use: file-scoped namespaces, primary constructors where the class is a thin
wrapper over its dependencies, `Handle` as the property name for a raw Vulkan handle, collection
expressions (`[]`, `[.. items]`), and exceptions carrying the failing `Result` in the message.
