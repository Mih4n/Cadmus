{ pkgs ? import <nixpkgs> {} }:
pkgs.mkShell {
  buildInputs = [
    pkgs.SDL2
    pkgs.vulkan-tools
    pkgs.vulkan-headers
    pkgs.vulkan-loader
  ];

  shellHook = ''
    export LD_LIBRARY_PATH=$LD_LIBRARY_PATH:${pkgs.SDL2}/lib
    export LD_LIBRARY_PATH=$LD_LIBRARY_PATH:${pkgs.vulkan-loader}/lib
  '';
}

