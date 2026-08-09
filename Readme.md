# **Cadmus Game Engine (WIP)**

![Logo](./Assets/Logo.svg)

---

**Cadmus** is a modular, component-based, Vulkan-powered game engine prototype written in **C#**.
The project is structured as a collection of loosely coupled libraries, making the engine easy to extend, test, and integrate into other applications.

This repository also contains a small demo project (**TestGame**) showing how to bootstrap a basic Cadmus application.

## 🗝️ **Key Features**

### **Modular Architecture**

Cadmus follows the **Entity–Component–System (ECS)** style, strictly:

* **Components** are data, with no behaviour
* **Entities** are containers of components, with no logic and no services of their own
* **Systems** hold all behaviour and find their entities by querying for component sets
* **Events** (`IEventQueue`) let systems cooperate without referring to each other

A scene only decides which entities exist; everything that happens per frame is a system.

### **Dependency Injection Everywhere**

Every part of the engine — systems, scenes, entities, the game itself and each Vulkan object — is
resolved from a `Microsoft.Extensions.DependencyInjection` container. Nothing reaches for an ambient
context or a service locator; a type declares what it needs in its constructor:

```csharp
public sealed class MainScene(IEntityFactory entities, IGameWindow window) : Scene(entities);
```

The container is built with `ValidateOnBuild`, so a wiring mistake fails at startup rather than mid-frame.

### **Vulkan Rendering Backend**

The engine uses **Silk.NET** for Vulkan and windowing:

* Instance, device, swapchain, render pass, framebuffers, pipeline and sync objects are all
  container singletons — creation order and reverse-order disposal come for free
* Swapchain recreation on resize, with dependent resources rebuilding themselves
* Per-draw model matrices via a dynamic uniform buffer, so a whole scene records into one command buffer
* Textures cached by path, one descriptor set each, with a fallback for missing files
* Optional validation layer, and `IFrameCapture` for saving a presented frame to PNG

### **Scene Management**

Scenes are registered by name and created per load from the container, each in its own service scope:

```csharp
builder.Services.AddScene<MainScene>("Main");
await Scenes.LoadAsync("Main");
```

### **Sprite System**

* Automatic quad mesh generation
* Pixel-space orthographic camera by default (origin top-left), perspective when you ask for it
* Local sprite offsets, rotation, per-sprite size and depth-sorted alpha blending
* Per-sprite tint, so one white texture covers every flat-coloured shape

### **Input**

`IInputService` reports held keys and per-frame edges (`WasKeyPressed`), injected like anything else.
The `Key` enum lives in `Cadmus.Core` and carries no backend types.

### **Diagnostics**

A built-in statistics HUD, toggled with **F3**: fps, frame time with min/max over a rolling window,
frame index, uptime, scene name, entity count, draw calls, resident textures and meshes, render
target size and GPU name. It reads `IFrameStatistics`, which any code can inject — the overlay is
just one consumer. Text is rendered from a bundled ASCII atlas, so the engine can draw strings
anywhere via `BitmapFont`.

### **Systems Pipeline**

Systems implement `ISystem`, are registered with `AddSystem<T>()` and run in `Order` order each
frame; `IRenderSystem.Render` submits afterwards.

---

## 📂 **Project Structure**

```
Cadmus/
  Cadmus.Core/       - Backend-agnostic interfaces and primitives
  Cadmus.Engine/     - Components, entities, scenes, geometry
  Cadmus.Graphics/   - Window, Vulkan objects, GPU resource cache
  Cadmus.Rendering/  - Render systems that turn a scene into draw calls
  Cadmus.App/        - Application builder, frame loop, Game base class
  TestGame/          - Minimal runnable example
```

---

## 🚀 **Getting Started**

```csharp
var builder = CadmusApplication.CreateBuilder();

builder.ConfigureWindow(window =>
{
    window.Title = "Cadmus";
    window.Width = 1280;
    window.Height = 720;
});

builder.Services.AddScene<MainScene>("Main");
builder.UseGame<SnakeGame>();

await using var app = builder.Build();
await app.RunAsync();
```

Running the demo. The project ships a [devenv](https://devenv.sh) shell that provides the .NET SDK,
GLFW, the Vulkan loader, validation layers and `glslc` — window creation fails outside it, because
GLFW resolves X11/Wayland at runtime:

```bash
devenv shell     # or run `direnv allow` once and just cd into the directory
run              # build + launch
doctor           # check the toolchain if something is off
```

---

## 🛠️ **Tech Stack**

* **C# 14 / .NET 10**
* **Silk.NET (Vulkan + Windowing)**
* **Microsoft.Extensions.DependencyInjection**
* **GLSL shaders (pre-compiled to SPIR-V)**
* **ECS-style architecture**

---

## 📌 **Current Status**

This engine is in an **early experimental stage**.
Sprite rendering, texture uploading and scene management work end to end; input, audio and physics
are still missing.

---

## 🧭 **Roadmap**

* [x] Proper Vulkan rendering pipeline
* [x] Texture uploading to GPU
* [x] Dependency injection across the engine
* [ ] Material/Shader abstraction
* [ ] Scene graph
* [ ] Input system
* [ ] UI layer
* [ ] Asset pipeline (importers)
* [ ] Editor tools

---

## 🤝 Contributions

Contributions, issues, and suggestions are welcome!
This project exists to explore engine architecture and Vulkan integration in C# — feedback is always appreciated.

---

## 📄 License

MIT

---