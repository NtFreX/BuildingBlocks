using NtFreX.BuildingBlocks.Mesh;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace NtFreX.BuildingBlocks.Light
{
    //TODO: support unlimited lights by doing multiple render passes and combining them (each render pass could support a fixed size of lights and output a texture, then we just need to combine the textures, there could be a stage which does just texture combining maybe it can support a batch for each pass)
    public class LightSystem : IDisposable
    {
        private LightInfo lightInfo = new LightInfo();
        private bool hasLightChanged = true;

        private readonly GraphicsDevice graphicsDevice;

        public event EventHandler LightChanged;

        public Vector4 AmbientLight { get => lightInfo.AmbientLight; set { lightInfo.AmbientLight = value; UpdateLightChanged(); } }
        public Vector3 DirectionalLightDirection { get => lightInfo.DirectionalLightDirection; set { lightInfo.DirectionalLightDirection = value; UpdateLightChanged(); } }
        public Vector4 DirectionalLightColor { get => lightInfo.DirectionalLightColor; set { lightInfo.DirectionalLightColor = value; UpdateLightChanged(); } }

        public DeviceBuffer LightBuffer { get; private set; }
        public ResourceSet LightInfoResourceSet { get; private set; }

        public LightSystem(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        {
            this.graphicsDevice = graphicsDevice;

            LightBuffer = resourceFactory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<LightInfo>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            var lightInfoLayout = ResourceLayoutFactory.GetLightInfoLayout(resourceFactory);
            LightInfoResourceSet = ResourceSetFactory.GetResourceSet(resourceFactory, new ResourceSetDescription(lightInfoLayout, LightBuffer));
        }

        // TODO: call this internal (use data structure for point lights with isactive flag)
        public void SetPointLights(params PointLightInfo[] lights)
        {
            if (LightInfo.MaxLights < lights.Length)
                throw new Exception($"Only {LightInfo.MaxLights} point lights are supported");

            lightInfo.ActivePointLights = lights.Length;
            for(var i = 0; i < lights.Length; i++)
            {
                lightInfo[i] = lights[i];
            }

            UpdateLightChanged();
        }

        public void Update()
        {
            if (hasLightChanged)
            {
                graphicsDevice.UpdateBuffer(LightBuffer, 0, lightInfo);
                hasLightChanged = false;
            }
        }

        public void Dispose()
        {
            LightBuffer.Dispose();
            LightInfoResourceSet.Dispose();
        }

        private void UpdateLightChanged()
        {
            LightChanged?.Invoke(this, EventArgs.Empty);
            hasLightChanged = true;
        }
    }
}
