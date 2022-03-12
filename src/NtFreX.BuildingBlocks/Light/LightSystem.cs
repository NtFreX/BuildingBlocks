using NtFreX.BuildingBlocks.Mesh.Factories;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace NtFreX.BuildingBlocks.Light
{
    //TODO: support unlimited lights by doing multiple render passes and combining them (each render pass could support a fixed size of lights and output a texture, then we just need to combine the textures, there could be a stage which does just texture combining maybe it can support a batch for each pass)
    public class LightSystem
    {
        private PointLightCollectionInfo pointLightInfo = new ();
        private DirectionalLightInfo directionalLightInfo = new();
        private bool hasLightChanged = true;
        private GraphicsDevice? graphicsDevice;

        public event EventHandler? LightChanged;

        public Vector4 AmbientLight { get => directionalLightInfo.AmbientLight; set { directionalLightInfo.AmbientLight = value; UpdateLightChanged(); } }
        public Vector3 DirectionalLightDirection { get => directionalLightInfo.DirectionalLightDirection; set { directionalLightInfo.DirectionalLightDirection = value; UpdateLightChanged(); } }
        public Vector4 DirectionalLightColor { get => directionalLightInfo.DirectionalLightColor; set { directionalLightInfo.DirectionalLightColor = value; UpdateLightChanged(); } }

        public DeviceBuffer? DirectionalLightBuffer { get; private set; }
        public DeviceBuffer? PointLightBuffer { get; private set; }
        public ResourceSet? LightInfoResourceSet { get; private set; }
        
        public void CreateDeviceResources(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        {
            Debug.Assert(this.graphicsDevice == null);

            this.graphicsDevice = graphicsDevice;

            PointLightBuffer = resourceFactory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<PointLightCollectionInfo>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            PointLightBuffer.Name = "PointLightBuffer";
            DirectionalLightBuffer = resourceFactory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<DirectionalLightInfo>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            DirectionalLightBuffer.Name = "DirectionalLightBuffer";

            var lightInfoLayout = ResourceLayoutFactory.GetLightInfoLayout(resourceFactory);
            LightInfoResourceSet = ResourceSetFactory.GetResourceSet(resourceFactory, new ResourceSetDescription(lightInfoLayout, DirectionalLightBuffer, PointLightBuffer), "LightInfoResourceSet");
        }

        public void DestroyDeviceResources()
        {
            graphicsDevice = null;

            PointLightBuffer?.Dispose();
            PointLightBuffer = null;

            DirectionalLightBuffer?.Dispose();
            DirectionalLightBuffer = null;

            LightInfoResourceSet?.Dispose();
            LightInfoResourceSet = null;
        }

        // TODO: call this internal (use data structure for point lights with isactive flag)
        public void SetPointLights(params PointLightInfo[] lights)
        {
            if (PointLightCollectionInfo.MaxLights < lights.Length)
                throw new Exception($"Only {PointLightCollectionInfo.MaxLights} point lights are supported");

            pointLightInfo.ActivePointLights = lights.Length;
            for(var i = 0; i < lights.Length; i++)
            {
                pointLightInfo[i] = lights[i];
            }

            UpdateLightChanged();
        }

        public void Update()
        {
            if (hasLightChanged && graphicsDevice != null)
            {
                //TODO: update only required buffer
                graphicsDevice.UpdateBuffer(PointLightBuffer, 0,  pointLightInfo);

                var directionalLight = new DirectionalLightInfo { AmbientLight = AmbientLight, DirectionalLightColor = DirectionalLightColor, DirectionalLightDirection = Vector3.Normalize(DirectionalLightDirection) };
                graphicsDevice.UpdateBuffer(DirectionalLightBuffer, 0, directionalLight);
                hasLightChanged = false;
            }
        }

        private void UpdateLightChanged()
        {
            LightChanged?.Invoke(this, EventArgs.Empty);
            hasLightChanged = true;
        }
    }
}
