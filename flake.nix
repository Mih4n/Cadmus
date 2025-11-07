{
    description = "Nix devshells!";

    inputs = {
        nixpkgs.url = "github:nixos/nixpkgs/nixpkgs-unstable";
    };

    outputs = { nixpkgs, ... }: let
        system = "x86_64-linux";
        pkgs = import nixpkgs { inherit system; };
    in {
        devShells.${system}.default = pkgs.mkShell {
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
        };
    };
}
