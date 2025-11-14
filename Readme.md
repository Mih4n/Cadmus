# **Cadmus Game Engine (WIP)**

![Logo](./Assets/Logo.svg)

---

**Cadmus** is a modular, component-based, Vulkan-powered game engine prototype written in **C#**.
The project is structured as a collection of loosely coupled libraries, making the engine easy to extend, test, and integrate into other applications.

This repository also contains a small demo project (**TestGame**) showing how to bootstrap a basic Cadmus application.

## 🗝️ **Key Features**

### **Modular Architecture**

Cadmus follows the **Entity–Component–System (ECS)** style, where:

* **Entities** contain composable components
* **Components** define data
* **Systems** process that data each frame

This provides flexibility, scalability, and clean separation of responsibilities.

### **Vulkan Rendering Backend**

The engine uses **Silk.NET** for Vulkan initialization, window handling, and GPU communication:

* Automatic Vulkan instance creation
* GPU-ready window context
* Shader-based rendering pipeline
* Basic sprite mesh + shaders included

### **Scene Management**

Scenes are:

* Fully composable
* Capable of holding entities
* Switchable at runtime
* Loadable/unloadable via `LoadSceneAsync`

### **Sprite System**

The engine includes a simple sprite component system for 2D rendering:

* Automatic quad mesh generation
* Model matrix calculation (position, scale, rotation)
* Texture loading system (WIP)

### **Systems Pipeline**

Each engine system — renderer, texture loader, etc. — runs independently via the global `IGameContext`.

---

## 📂 **Project Structure**

```
Cadmus/
  Cadmus.App/              - Game base class & core runtime
  Cadmus.Domain/           - Components, entities, game logic
  Cadmus.Domain.Contracts/ - Interfaces shared across modules
  Cadmus.Render/           - Shaders & rendering utilities
  Cadmus.Systems/          - Systems (VulkanRenderer, TextureLoadSystem)
  TestGame/                - Minimal runnable example
```

---

## 🧱 **Core Concepts**

### 🧩 **Entities & Components**

All game objects inherit from `Entity`, a compose-component container.
Example components include:

* `SpriteComponent`
* `PositionComponent`
* `Mesh`
* `VulkanRenderingContext`

### 🎮 **Game Loop**

`Game` is an abstract base class that:

* Initializes Vulkan
* Registers systems
* Processes them on each update
* Manages scenes

### 🌄 **Rendering**

Includes:

* GLSL shaders (`sprite.vert`, `sprite.frag`)
* Sprite UV/mesh generation
* Model matrix calculation

---

## 🧪 **Test Game**

`TestGame` shows:

* Creating a custom game class
* Setting up scenes
* Adding entities
* Attaching sprite components

Use it as a starting point for building your own gameplay.

---

## 🛠️ **Tech Stack**

* **C# 14 / .NET 10**
* **Silk.NET (Vulkan + Windowing)**
* **GLSL shaders**
* **ECS-style architecture**

---

## 📌 **Current Status**

This engine is in an **early experimental stage**.
Several subsystems (render pipeline, texture handling, input, audio, physics) are missing or incomplete.

---

## 🧭 **Roadmap**

* [ ] Proper Vulkan rendering pipeline
* [ ] Texture uploading to GPU
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