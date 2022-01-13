using Microsoft.Extensions.Logging;
using NtFreX.BuildingBlocks.Cameras;
using NtFreX.BuildingBlocks.Input;
using NtFreX.BuildingBlocks.Light;
using NtFreX.BuildingBlocks.Models;
using NtFreX.BuildingBlocks.Standard;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks
{
    public class GraphicsSystem
    {

        private readonly DebugExecutionTimerSource timerGetVisibleObjects;
        private readonly List<Model> models = new List<Model>();
                
        public Camera Camera { get; private set; }
        public LightSystem LightSystem { get; private set; }

        public GraphicsSystem(ILoggerFactory loggerFactory, ResourceFactory resourceFactory, Camera camera)
        {
            Camera = camera;
            LightSystem = new LightSystem(resourceFactory);
            timerGetVisibleObjects = new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystem GetVisibleObjects");
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
            
            var timer = new DebugExecutionTimer(timerGetVisibleObjects);
            var visibleModels = models.Where(x => frustum.Contains(x.GetBoundingBox()) != ContainmentType.Disjoint && x.MeshBuffer.Material.Value.Opacity != 0f).ToArray();
            timer.Dispose();

            //var queue = new RenderQueue();
            //queue.AddRange(visibleModels, Camera.Position);
            //queue.Sort();

            //foreach (var model in queue)
            //{
            //    model.Draw(commandList);
            //}

            //if(visibleModels.TryGetValue(true, out var opaqueModels))
            //{
            //    var transparentQueue = new RenderQueue();
            //    transparentQueue.AddRange()
            //    foreach (var model in opaqueModels)
            //    {
            //        model.Draw(commandList);
            //    }
            //}
            //if(visibleModels.TryGetValue(false, out var transparentModels))
            //{
            //    foreach(var model in transparentModels)
            //    {
            //        model.Draw(commandList);
            //    }
            //}
            //TODO: not nessesary to order opaque models
            var transparentQueue = new RenderQueue();
            var opaqueQueue = new RenderQueue();
            foreach (var model in visibleModels)
            {
                if (model.MeshBuffer.Material.Value.Opacity != 1f)
                {
                    transparentQueue.Add(model, Camera.Position);
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
