using NtFreX.BuildingBlocks.Cameras;
using NtFreX.BuildingBlocks.Desktop;
using NtFreX.BuildingBlocks.Input;
using NtFreX.BuildingBlocks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks
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

    public class GraphicsSystem
    {
        private readonly List<Model> models = new List<Model>();
                
        public Camera Camera { get; private set; }
        public LightSystem LightSystem { get; private set; }

        public GraphicsSystem(ResourceFactory resourceFactory, Camera camera)
        {
            Camera = camera;
            LightSystem = new LightSystem(resourceFactory);
        }

        public void AddModels(params Model[] models)
        {
            this.models.AddRange(models);
        }

        public void Update(GraphicsDevice graphicsDevice, float deltaSeconds, InputHandler inputHandler)
        {
            Camera?.Update(graphicsDevice, inputHandler, deltaSeconds);

            foreach(var model in models)
            {
                model.Update(graphicsDevice, inputHandler, deltaSeconds);
            }

            LightSystem.Update(graphicsDevice);
        }


        public void Draw(CommandList commandList)
        {
            if (Camera == null)
                return;

            // TODO: render passess
            var frustum = new BoundingFrustum(Camera.ViewMatrix * Camera.ProjectionMatrix);
            var visibleModels = models.Where(x => frustum.Contains(x.BoundingBox) != ContainmentType.Disjoint).ToArray();

            //var queue = new RenderQueue();
            //queue.AddRange(visibleModels, Camera.Position);
            //queue.Sort();

            //foreach (var model in queue)
            //{
            //    model.Draw(commandList);
            //}

            var transparentQueue = new RenderQueue();
            var opaqueQueue = new RenderQueue();
            foreach (var model in visibleModels)
            {
                if (model.Material.Value.Opacity != 1f)
                {
                    if (model.Material.Value.Opacity != 0f)
                    {
                        transparentQueue.Add(model, Camera.Position);
                    }
                }
                else
                {
                    opaqueQueue.Add(model, Camera.Position);
                }
            }

            transparentQueue.Sort();
            opaqueQueue.Sort();


            foreach (var model in opaqueQueue.Reverse())
            {
                model.Draw(commandList);
            }
            foreach (var model in transparentQueue.Reverse())
            {
                model.Draw(commandList);
            }
        }

        public void OnWindowResized(int width, int height)
        {
            if (Camera == null)
                return;

            Camera.WindowWidth.Value = width;
            Camera.WindowHeight.Value = height;
        }
    }
}
