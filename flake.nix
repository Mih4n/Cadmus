{
  description = "Cadmus development environment";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs/nixpkgs-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs = { nixpkgs, flake-utils, ... }:
    flake-utils.lib.eachDefaultSystem (system:
      let
        pkgs = nixpkgs.legacyPackages.${system};
      in
      {
        devShells.default = pkgs.mkShell {
          buildInputs = with pkgs; [
            SDL2
            gtk3
            glfw
            vulkan-tools
            vulkan-headers
            vulkan-loader
            glslang
            libGL
            libxi
            libxcursor
            libxrandr
            libxinerama
            spirv-tools
            shaderc
            pkg-config
          ];

          shellHook = ''
            export LD_LIBRARY_PATH="$LD_LIBRARY_PATH:${pkgs.glfw}/lib:${pkgs.vulkan-loader}/lib:${pkgs.libGL}/lib:${pkgs.SDL2}/lib"
            
            echo "Cadmus development environment ready!"
            echo "Running on: $XDG_SESSION_TYPE"
            echo "Available displays:"
            echo "  DISPLAY: $DISPLAY"
            echo "  WAYLAND_DISPLAY: $WAYLAND_DISPLAY"
          '';
        };
      }
    );
}