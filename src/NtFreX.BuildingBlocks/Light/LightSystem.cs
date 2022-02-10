using NtFreX.BuildingBlocks.Desktop;
using NtFreX.BuildingBlocks.Mesh;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace NtFreX.BuildingBlocks.Light
{
    public class LightSystem : IDisposable
    {
        private LightInfo lightInfo = new LightInfo();
        private bool hasLightChanged = true;

        private readonly GraphicsDevice graphicsDevice;

        public Vector3 AmbientLight { get => lightInfo.AmbientLight; set => lightInfo.AmbientLight = value; }

        public DeviceBuffer LightBuffer { get; private set; }
        public ResourceSet LightInfoResourceSet { get; private set; }

        public LightSystem(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        {
            this.graphicsDevice = graphicsDevice;

            LightBuffer = resourceFactory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<LightInfo>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            var lightInfoLayout = ResourceLayoutFactory.GetLightInfoLayout(resourceFactory);
            LightInfoResourceSet = ResourceSetFactory.GetResourceSet(resourceFactory, new ResourceSetDescription(lightInfoLayout, LightBuffer));
        }

        public void SetPointLights(params PointLightInfo[] lights)
        {
            if (LightInfo.MaxLights < lights.Length)
                throw new Exception($"Only {LightInfo.MaxLights} point lights are supported");

            lightInfo.ActivePointLights = lights.Length;
            for(var i = 0; i < lights.Length; i++)
            {
                lightInfo[i] = lights[i];
            }

            hasLightChanged = true;
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
    }
}
