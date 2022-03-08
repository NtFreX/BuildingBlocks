using System.Diagnostics;
using Veldrid;

namespace NtFreX.BuildingBlocks.Material
{
    internal class MaterialTexture
    {
        public string Name { get; }
        public uint Size { get; }
        public MaterialNode[] MaterialNodes { get; }
        public TextureView? Output => MaterialNodes.Last().Output;

        public MaterialTexture(MaterialNode[] materialNodes, uint size, string name)
        {
            this.MaterialNodes = materialNodes;
            this.Size = size;
            this.Name = name;
        }

        public void CreateDeviceResources(TextureView input, GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        {
            var currentInput = input;
            foreach (var node in MaterialNodes)
            {
                node.Input = currentInput;
                node.CreateDeviceResources(graphicsDevice, resourceFactory);
                
                Debug.Assert(node.Output != null);
                currentInput = node.Output;
            }
        }
        public void DestroyDeviceResources()
        {
            foreach (var node in MaterialNodes)
            {
                node.DestroyDeviceResources();
                node.Input = null;
            }
        }

        public void Run(CommandList commandList, float delta)
        {
            foreach(var node in MaterialNodes)
            {
                node.Run(commandList, delta);
            }
        }
    }
}
