using Microsoft.Extensions.Logging;
using NtFreX.BuildingBlocks.Cameras;
using NtFreX.BuildingBlocks.Input;
using NtFreX.BuildingBlocks.Light;
using NtFreX.BuildingBlocks.Mesh;
using NtFreX.BuildingBlocks.Standard;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks
{
    public class GraphicsSystem : IDisposable
    {
        private readonly DebugExecutionTimer timerGetVisibleObjects;
        private readonly List<Model> frustumItems = new List<Model>();
        private readonly HashSet<Model> allItems = new HashSet<Model>();
        private readonly Octree<Model> models = new Octree<Model>(new BoundingBox(new Vector3(float.MinValue), new Vector3(float.MaxValue)), 2);


        public Model[] Models => allItems.ToArray();

        // TODO: support empty camera
        public Mutable<Camera?> Camera { get; }
        public LightSystem LightSystem { get; set; }

        private BoundingFrustum currentBoundingFrustum = new BoundingFrustum();

        public GraphicsSystem(ILoggerFactory loggerFactory, GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Camera camera)
        {
            Camera = new Mutable<Camera?>(camera, this);
            LightSystem = new LightSystem(graphicsDevice, resourceFactory);
            timerGetVisibleObjects = new DebugExecutionTimer(new DebugExecutionTimerSource(loggerFactory.CreateLogger<DebugExecutionTimerSource>(), "GraphicsSystem GetVisibleObjects"));
        }

        private void UpdateModelActive(object? sender, bool visible)
        {
            var model = (Model)sender!;
            if (!visible)
            {
                RemoveModelCore(model);
            }
            else
            {
                AddModelCore(model);
            }
        }

        private void UpdateModelMaterial(object? sender, MaterialInfo material)
        {
            var model = (Model)sender!;
            if (material.Opacity == 0)
            {
                RemoveModelCore(model);
            }
            else
            {
                AddModelCore(model);
            }
        }

        private void RemoveModelCore(Model model)
        {
            if (!allItems.Contains(model))
                return;

            models.RemoveItem(model);
            allItems.Remove(model);
            model.NewBoundingBoxAvailable -= UpdateModelBounds;
        }

        private void AddModelCore(Model model)
        {
            if (allItems.Contains(model))
                return;

            models.AddItem(model.GetBoundingBox(), model);
            allItems.Add(model);
            model.NewBoundingBoxAvailable += UpdateModelBounds;
        }

        private void UpdateModelBounds(object? sender, EventArgs args)
        {
            var model = (Model)sender!;
            this.models.MoveItem(model, model.GetBoundingBox());
        }

        public void RemoveModels(params Model[] models)
        {
            foreach (var model in models)
            {
                if (!allItems.Contains(model))
                    continue;

                model.IsActive.ValueChanged -= UpdateModelActive;
                model.MaterialChanged -= UpdateModelMaterial;
                RemoveModelCore(model);
            }
        }
        public void AddModels(params Model[] models)
        {
            foreach (var model in models)
            {
                if (allItems.Contains(model))
                    continue;

                model.IsActive.ValueChanged += UpdateModelActive;
                model.MaterialChanged += UpdateModelMaterial;
                AddModelCore(model);
            }
        }

        public void Update(float deltaSeconds, InputHandler inputHandler)
        {
            Camera.Value?.BeforeModelUpdate(inputHandler, deltaSeconds);

            foreach(var model in allItems)
            {
                model.Update(inputHandler, deltaSeconds);
            }

            Camera.Value?.AfterModelUpdate(inputHandler, deltaSeconds);

            LightSystem.Update();
        }


        public void Draw(CommandList commandList)
        {
            if (Camera == null || Camera.Value == null)
                return;

            // TODO: render passess

            var transparentQueue = new RenderQueue();
            var opaqueQueue = new RenderQueue();
            {
                timerGetVisibleObjects.Start();

                currentBoundingFrustum = new BoundingFrustum(Camera.Value.ViewMatrix * Camera.Value.ProjectionMatrix);

                frustumItems.Clear();
                models.GetContainedObjects(currentBoundingFrustum, frustumItems);

                for (var i = 0; i < frustumItems.Count; i++)
                {
                    var model = frustumItems[i];
                    var opacity = model.MeshBuffer.Material.Value.Opacity;
                    if (opacity != 1f)
                    {
                        transparentQueue.Add(model, Camera.Value.Position);
                    }
                    else
                    {
                        opaqueQueue.Add(model, Camera.Value.Position);
                    }
                }

                timerGetVisibleObjects.Stop();
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
            if (Camera.Value == null)
                return;

            Camera.Value.WindowWidth.Value = width;
            Camera.Value.WindowHeight.Value = height;
        }

        public void Dispose()
        {
            LightSystem.Dispose();
        }
    }
}
