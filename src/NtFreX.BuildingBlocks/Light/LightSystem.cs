using NtFreX.BuildingBlocks.Desktop;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace NtFreX.BuildingBlocks.Light
{
    public class LightSystem
    {
        private LightInfo lightInfo = new LightInfo();

        public Vector3 AmbientLight { get => lightInfo.AmbientLight; set => lightInfo.AmbientLight = value; }

        public DeviceBuffer LightBuffer { get; private set; }

        public LightSystem(ResourceFactory resourceFactory)
        {
            LightBuffer = resourceFactory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<LightInfo>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));
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
        }

        public void Update(GraphicsDevice graphicsDevice)
        {
            // TODO: update buffer only when nessesary
            graphicsDevice.UpdateBuffer(LightBuffer, 0, lightInfo);
        }
    }
}
