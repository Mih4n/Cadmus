{ pkgs, lib, ... }:

let
  # GLFW opens X11/Wayland/Vulkan through dlopen at runtime, and the copy Silk.NET ships in
  # runtimes/linux-x64/native carries no RPATH. Without these on LD_LIBRARY_PATH glfwInit fails and
  # Silk.NET reports the unhelpful "Couldn't find a suitable window platform (GlfwPlatform - not
  # applicable)". Listing them here is what actually prevents that error.
  runtimeLibraries = with pkgs; [
    glfw
    vulkan-loader
    libGL

    # X11 (also used under XWayland)
    xorg.libX11
    xorg.libXcursor
    xorg.libXext
    xorg.libXi
    xorg.libXinerama
    xorg.libXrandr
    xorg.libXxf86vm

    # Native Wayland
    wayland
    libxkbcommon
  ];
in
{
  name = "cadmus";

  packages = with pkgs; [
    # Vulkan tooling
    vulkan-tools            # vulkaninfo, vkcube
    vulkan-validation-layers
    vulkan-headers

    # Shader compilation (Assets/Shaders/*.spv)
    shaderc                 # glslc
    glslang
    spirv-tools

    pkg-config
  ] ++ runtimeLibraries;

  languages.dotnet = {
    enable = true;
    package = pkgs.dotnetCorePackages.sdk_10_0;
  };

  env = {
    # mkForce because languages.dotnet also defines this; icu is carried over from that module,
    # .NET needs it for globalization.
    LD_LIBRARY_PATH = lib.mkForce (lib.makeLibraryPath (runtimeLibraries ++ [ pkgs.icu ]));

    # Makes VK_LAYER_KHRONOS_validation resolvable, so VulkanOptions.EnableValidation actually
    # reports errors instead of logging that the layer is missing.
    VK_LAYER_PATH = "${pkgs.vulkan-validation-layers}/share/vulkan/explicit_layer.d";

    DOTNET_CLI_TELEMETRY_OPTOUT = "1";
    DOTNET_NOLOGO = "1";
  };

  scripts = {
    build.exec = ''
      dotnet build "''${DEVENV_ROOT}/Cadmus.sln" "$@"
    '';

    run.exec = ''
      dotnet build "''${DEVENV_ROOT}/Cadmus.sln" || exit 1
      cd "''${DEVENV_ROOT}/.Output/TestGame" && exec dotnet TestGame.dll "$@"
    '';

    # Renders ~95 frames, writes a PNG and exits — useful without a visible display session.
    screenshot.exec = ''
      target="''${1:-''${DEVENV_ROOT}/.Output/frame.png}"
      dotnet build "''${DEVENV_ROOT}/Cadmus.sln" || exit 1
      cd "''${DEVENV_ROOT}/.Output/TestGame" && CADMUS_SCREENSHOT="$target" exec dotnet TestGame.dll
    '';

    # Recompiles GLSL to the committed SPIR-V.
    shaders.exec = ''
      shaders="''${DEVENV_ROOT}/Cadmus.Graphics/Assets/Shaders"
      for source in "$shaders"/*.vert "$shaders"/*.frag; do
        [ -e "$source" ] || continue
        echo "glslc $(basename "$source")"
        glslc "$source" -o "$source.spv" || exit 1
      done
    '';

    doctor.exec = ''
      echo "dotnet     : $(dotnet --version 2>/dev/null || echo MISSING)"
      echo "glslc      : $(glslc --version 2>/dev/null | head -1 || echo MISSING)"
      echo "session    : ''${XDG_SESSION_TYPE:-unknown} (DISPLAY=''${DISPLAY:-unset} WAYLAND_DISPLAY=''${WAYLAND_DISPLAY:-unset})"
      if [ -z "''${DISPLAY}" ] && [ -z "''${WAYLAND_DISPLAY}" ]; then
        echo "  ! no display: windowed runs will fail, use 'screenshot' instead"
      fi
      echo -n "vulkan     : "
      vulkaninfo --summary 2>/dev/null | grep -m1 deviceName || echo "MISSING (vulkaninfo failed)"
      echo -n "validation : "
      if [ -r "''${VK_LAYER_PATH}/VkLayer_khronos_validation.json" ]; then echo "available"; else echo "MISSING"; fi
    '';
  };

  enterShell = ''
    echo "Cadmus — build | run | screenshot [path] | shaders | doctor"
  '';
}
