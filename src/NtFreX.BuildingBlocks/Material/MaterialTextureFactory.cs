using System.Diagnostics;
using Veldrid;

using VeldridTexture = Veldrid.Texture;

namespace NtFreX.BuildingBlocks.Material
{
    public class MaterialTextureFactory
    {
        private record TextureCollection(uint Size, TextureView InputTextureView, VeldridTexture InputTexture);

        private GraphicsDevice? graphicsDevice;
        private ResourceFactory? resourceFactory;
        private CommandList? commandList;

        private readonly List<TextureCollection> textureViews = new();
        private readonly List<MaterialTexture> textures = new ();

        public static MaterialTextureFactory Instance { get; } = new MaterialTextureFactory();

        private MaterialTextureFactory() { }

        private TextureView CreateInputTexture(uint size)
        {
            Debug.Assert(resourceFactory != null);

            var inputTexture = resourceFactory.CreateTexture(TextureDescription.Texture2D(
                size,
                size,
                1,
                1,
                PixelFormat.R32_G32_B32_A32_Float,
                TextureUsage.Sampled | TextureUsage.Storage));

            var inputTextureView = resourceFactory.CreateTextureView(inputTexture);
            textureViews.Add(new TextureCollection(size, inputTextureView, inputTexture));
            return inputTextureView;
        }

        private TextureView GetOrCreateInputTexture(uint size)
        {
            var collection = textureViews.FirstOrDefault(x => x.Size == size);
            if (collection == null)
            {
                return CreateInputTexture(size);
            }
            else
            {
                return collection.InputTextureView;
            }
        }

        public async Task TryCreateMaterialTextureAsync(string identifier, uint textureSize, params MaterialNode[] materialNodes)
        {
            var matText = textures.FirstOrDefault(t => t.Name == identifier);
            if (matText != null)
            {
                if (matText.Size != textureSize)
                    throw new Exception("There is already a texture with the same name and another size");

                return;
            }
            
            var texture = new MaterialTexture(materialNodes, textureSize, identifier);
            if(graphicsDevice != null)
            {
                Debug.Assert(resourceFactory != null);

                var textureView = GetOrCreateInputTexture(textureSize);
                // todo: grouped await
                await texture.CreateDeviceResourcesAsync(textureView, graphicsDevice, resourceFactory);
            }

            textures.Add(texture);
        }

        public async Task CreateDeviceResourcesAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        {
            this.graphicsDevice = graphicsDevice;
            this.resourceFactory = resourceFactory;

            commandList = resourceFactory.CreateCommandList();

            foreach (var texture in textures)
            {
                var textureView = GetOrCreateInputTexture(texture.Size);
                // todo: grouped await
                await texture.CreateDeviceResourcesAsync(textureView, graphicsDevice, resourceFactory);
            }
        }

        public void DestroyDeviceResources()
        {
            this.graphicsDevice = null;
            this.resourceFactory = null;

            foreach (var texture in textures)
            {
                texture.DestroyDeviceResources();
            }

            commandList?.Dispose();
            commandList = null;

            foreach(var item in textureViews)
            {
                item.InputTextureView.Dispose();
                item.InputTexture.Dispose();
            }
            textureViews.Clear();
        }

        public void Run(float delta)
        {
            Debug.Assert(commandList != null);
            Debug.Assert(graphicsDevice != null);

            commandList.Begin();
            foreach(var texture in textures)
            {
                texture.Run(commandList, delta);
            }
            commandList.End();
            graphicsDevice.SubmitCommands(commandList);
        }

        public TextureView GetOutput(string identifier)
        {
            var matText = textures.First(t => t.Name == identifier);
            var output = matText.Output;
            if (output == null)
                throw new Exception();

            return output;
        }
    }
}
