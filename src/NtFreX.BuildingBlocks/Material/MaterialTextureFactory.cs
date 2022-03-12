using SixLabors.ImageSharp;
using System.Diagnostics;
using Veldrid;
using VeldridTexture = Veldrid.Texture;

namespace NtFreX.BuildingBlocks.Material
{
    public class MaterialTextureFactory
    {
        private SemaphoreSlim locker = new (1);

        private record TextureCollection(Size Size, TextureView InputTextureView, VeldridTexture InputTexture);

        private GraphicsDevice? graphicsDevice;
        private ResourceFactory? resourceFactory;
        private CommandList? commandList;
        private Fence? fence;

        private readonly List<TextureCollection> textureViews = new();
        private readonly List<MaterialTexture> textures = new ();

        public static MaterialTextureFactory Instance { get; } = new MaterialTextureFactory();

        private MaterialTextureFactory() { }

        private TextureView CreateInputTexture(string identifier, Size size)
        {
            Debug.Assert(resourceFactory != null);

            var inputTexture = resourceFactory.CreateTexture(TextureDescription.Texture2D(
                (uint)size.Width,
                (uint)size.Height,
                1,
                1,
                PixelFormat.R32_G32_B32_A32_Float,
                TextureUsage.Sampled | TextureUsage.Storage));
            inputTexture.Name = "material_inputtexture_" + identifier;

            var inputTextureView = resourceFactory.CreateTextureView(inputTexture);
            inputTextureView.Name = "material_inputtextureview_" + identifier;
            textureViews.Add(new TextureCollection(size, inputTextureView, inputTexture));
            return inputTextureView;
        }

        private TextureView GetOrCreateInputTexture(string identifier, Size size)
        {
            var collection = textureViews.FirstOrDefault(x => x.Size == size);
            if (collection == null)
            {
                return CreateInputTexture(identifier, size);
            }
            else
            {
                return collection.InputTextureView;
            }
        }

        public void TryDestroyTexture(string identifier)
        {
            var texture = textures.FirstOrDefault(x => x.Name == identifier);
            if (texture == null)
                return;

            locker.Wait();
            try 
            {
                texture.DestroyDeviceResources();
                textures.Remove(texture);
            }
            finally
            {
                locker.Release();
            }
        }

        public Task TryCreateMaterialTextureAsync(string identifier, uint textureSize, params MaterialNode[] materialNodes)
            => TryCreateMaterialTextureAsync(identifier, new Size((int)textureSize, (int)textureSize), materialNodes);

        public async Task TryCreateMaterialTextureAsync(string identifier, Size textureSize, params MaterialNode[] materialNodes)
        {
            var matText = textures.FirstOrDefault(t => t.Name == identifier);
            if (matText != null)
            {
                if (matText.Size != textureSize)
                    throw new Exception("There is already a texture with the same name and another size");

                return;
            }

            await locker.WaitAsync();
            try 
            {
                var texture = new MaterialTexture(materialNodes, textureSize, identifier);
                if (graphicsDevice != null)
                {
                    Debug.Assert(resourceFactory != null);

                    var textureView = GetOrCreateInputTexture(identifier, textureSize);
                    // todo: grouped await
                    await texture.CreateDeviceResourcesAsync(textureView, graphicsDevice, resourceFactory);
                }

                textures.Add(texture);
            }
            finally
            {
                locker.Release();
            }
        }

        public async Task CreateDeviceResourcesAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        {
            if (this.graphicsDevice == graphicsDevice)
                return;

            this.graphicsDevice = graphicsDevice;
            this.resourceFactory = resourceFactory;

            commandList = resourceFactory.CreateCommandList();
            commandList.Name = "Material";

            fence = resourceFactory.CreateFence(false);
            fence.Name = "Material";

            await locker.WaitAsync();
            try
            {
                foreach (var texture in textures)
                {
                    var textureView = GetOrCreateInputTexture(texture.Name, texture.Size);
                    // todo: grouped await
                    await texture.CreateDeviceResourcesAsync(textureView, graphicsDevice, resourceFactory);
                }
            }
            finally
            {
                locker.Release();
            }
        }

        public void DestroyDeviceResources()
        {
            this.graphicsDevice = null;
            this.resourceFactory = null;

            locker.Wait();
            try
            {
                foreach (var texture in textures)
                {
                    texture.DestroyDeviceResources();
                }

                commandList?.Dispose();
                commandList = null;

                fence?.Dispose();
                fence = null;

                foreach (var item in textureViews)
                {
                    item.InputTextureView.Dispose();
                    item.InputTexture.Dispose();
                }
                textureViews.Clear();
            }
            finally
            {
                locker.Release();
            }
        }

        public void Run(float delta)
        {
            Debug.Assert(commandList != null);
            Debug.Assert(fence != null);
            Debug.Assert(graphicsDevice != null);

            locker.Wait();
            try
            {
                commandList.Begin();
                foreach (var texture in textures)
                {
                    texture.Run(commandList, delta);
                }
                commandList.End();
                graphicsDevice.SubmitCommands(commandList, fence);
                graphicsDevice.WaitForFence(fence);
                fence.Reset();
            }
            finally
            {
                locker.Release();
            }
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
