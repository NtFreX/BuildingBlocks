using NtFreX.BuildingBlocks.Cameras;
using NtFreX.BuildingBlocks.Light;
using NtFreX.BuildingBlocks.Model.Common;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Pools;
using System.Diagnostics;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks.Model
{
    public class Scene
    {
        private readonly Octree<Renderable> frustumTree = new (boundingBox: new (min: new (float.MinValue), max: new(float.MaxValue)), maxChildren: 2);
        private readonly HashSet<Renderable> freeRenderables = new ();
        private readonly HashSet<CullRenderable> cullRenderables = new ();
        public readonly HashSet<IUpdateable> Updateables = new ();

        private GraphicsDevice? graphicsDevice;
        private ResourceFactory? resourceFactory;
        private RenderContext? renderContext;
        private CommandListPool? commandListPool;

        public Mutable<Camera?> Camera { get; }
        public Mutable<LightSystem?> LightSystem { get; }

        public Renderable[] FreeRenderables => freeRenderables.ToArray();
        public CullRenderable[] CullRenderables => cullRenderables.ToArray();

        public Scene(bool isDebug = false)
        {
            Camera = new Mutable<Camera?>(null, this);
            Camera.ValueChanging += (_, args) => UpdateCamera(args.OldValue, args.NewValue);

            LightSystem = new Mutable<LightSystem?>(null, this);
            LightSystem.ValueChanging += (_, args) => UpdateLightSystem(args.OldValue, args.NewValue);

            AddFreeRenderableCore(new ScreenDuplicator(isDebug));
            AddFreeRenderableCore(new FullScreenQuad(isDebug));
        }

        internal void DestroyAllDeviceObjects()
        {
            this.graphicsDevice = null;
            this.resourceFactory = null;
            this.renderContext = null;
            this.commandListPool = null;

            foreach (CullRenderable cr in cullRenderables)
            {
                cr.DestroyDeviceObjects();
            }
            foreach (Renderable r in freeRenderables)
            {
                r.DestroyDeviceObjects();
            }

            LightSystem.Value?.DestroyDeviceResources();
            Camera.Value?.DestroyDeviceResources();
        }

        internal async Task CreateDeviceObjectsAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, RenderContext renderContext, CommandListPool commandListPool)
        {
            if (this.graphicsDevice != null)
            {
                Debug.Assert(this.graphicsDevice == graphicsDevice);
                return;
            }

            this.graphicsDevice = graphicsDevice;
            this.resourceFactory = resourceFactory;
            this.renderContext = renderContext;
            this.commandListPool = commandListPool;

            LightSystem.Value?.CreateDeviceResources(graphicsDevice, resourceFactory);
            Camera.Value?.CreateDeviceResources(graphicsDevice, resourceFactory);

            var cl = CommandListPool.TryGet(resourceFactory, commandListPool: commandListPool);
            var tasks = new List<Task>();

            foreach (CullRenderable cr in cullRenderables)
            {
                tasks.Add(cr.CreateDeviceObjectsAsync(graphicsDevice, resourceFactory, cl.CommandList, renderContext, this));
            }
            foreach (Renderable r in freeRenderables)
            {
                tasks.Add(r.CreateDeviceObjectsAsync(graphicsDevice, resourceFactory, cl.CommandList, renderContext, this));
            }

            await Task.WhenAll(tasks);
            CommandListPool.TrySubmit(graphicsDevice, cl, commandListPool);
        }

        public void GetContainedRenderables(BoundingFrustum frustum, List<Renderable> renderables)
        {
            frustumTree.GetContainedObjects(frustum, renderables);
            renderables.AddRange(freeRenderables);
        }

        public void AddUpdateables(params IUpdateable[] models)
        {
            foreach (var updateable in models)
            {
                if (Updateables.Contains(updateable))
                    continue;
                Updateables.Add(updateable);
            }
        }

        public void RemoveUpdateables(params IUpdateable[] models)
        {
            foreach (var updateable in models)
            {
                if (!Updateables.Contains(updateable))
                    continue;

                Updateables.Remove(updateable);
            }
        }

        public async Task AddFreeRenderablesAsync(params Renderable[] models)
        {
            var tasks = new List<Task>();
            foreach (var model in models)
            {
                if (freeRenderables.Contains(model))
                    continue;

                if (graphicsDevice != null)
                    tasks.Add(CreateDeviceObjectsAsync(model));

                model.ShouldRenderHasChanged += UpdateShouldRender;
                if (model.ShouldRender)
                    AddFreeRenderableCore(model);
            }

            await Task.WhenAll(tasks);
        }

        public void RemoveFreeRenderables(params Renderable[] models)
        {
            foreach (var model in models)
            {
                if (!freeRenderables.Contains(model))
                    continue;

                model.DestroyDeviceObjects();
                model.ShouldRenderHasChanged -= UpdateShouldRender;
                RemoveFreeRenderableCore(model);
            }
        }

        public void RemoveCullRenderables(params CullRenderable[] models)
        {
            foreach (var model in models)
            {
                if (!cullRenderables.Contains(model))
                    continue;

                model.DestroyDeviceObjects();
                model.ShouldRenderHasChanged -= UpdateCullableShouldRender;
                RemoveCullRenderableCore(model);
            }
        }

        public async Task AddCullRenderablesAsync(params CullRenderable[] models)
        {
            var tasks = new List<Task>();
            foreach (var model in models)
            {
                if (cullRenderables.Contains(model))
                    continue;

                if (graphicsDevice != null)
                    tasks.Add(CreateDeviceObjectsAsync(model));

                model.ShouldRenderHasChanged += UpdateCullableShouldRender;
                if (model.ShouldRender)
                    AddCullRenderableCore(model);
            }

            await Task.WhenAll(tasks);
        }

        private void AddFreeRenderableCore(Renderable model)
        {
            if (freeRenderables.Contains(model))
                return;

            freeRenderables.Add(model);
        }

        private void RemoveFreeRenderableCore(Renderable model)
        {
            if (!freeRenderables.Contains(model))
                return;

            freeRenderables.Remove(model);
        }

        private void AddCullRenderableCore(CullRenderable model)
        {
            if (cullRenderables.Contains(model))
                return;

            frustumTree.AddItem(model.GetBoundingBox(), model);
            cullRenderables.Add(model);

            model.NewBoundingBoxAvailable += UpdateCullableBounds;
        }

        private void RemoveCullRenderableCore(CullRenderable model)
        {
            if (!cullRenderables.Contains(model))
                return;

            frustumTree.RemoveItem(model);
            cullRenderables.Remove(model);
            model.NewBoundingBoxAvailable -= UpdateCullableBounds;
        }

        private async Task CreateDeviceObjectsAsync(Renderable model)
        {
            Debug.Assert(graphicsDevice != null);
            Debug.Assert(resourceFactory != null);
            Debug.Assert(renderContext != null);

            var cl = CommandListPool.TryGet(resourceFactory, commandListPool: commandListPool);
            await model.CreateDeviceObjectsAsync(graphicsDevice, resourceFactory, cl.CommandList, renderContext, this);
            CommandListPool.TrySubmit(graphicsDevice, cl, commandListPool);
        }

        private void UpdateCullableBounds(object? sender, EventArgs args)
        {
            var model = (CullRenderable)sender!;
            frustumTree.MoveItem(model, model.GetBoundingBox());
        }

        private void UpdateCullableShouldRender(object? sender, bool shouldRender)
        {
            var model = (CullRenderable)sender!;
            if (shouldRender)
            {
                AddCullRenderableCore(model);
            }
            else
            {
                RemoveCullRenderableCore(model);
            }
        }

        private void UpdateShouldRender(object? sender, bool shouldRender)
        {
            var model = (Renderable)sender!;
            if (shouldRender)
            {
                AddFreeRenderableCore(model);
            }
            else
            {
                RemoveFreeRenderableCore(model);
            }
        }

        private void UpdateCamera(Camera? oldCamera, Camera? newCamera)
        {
            oldCamera?.DestroyDeviceResources();

            if (graphicsDevice != null && resourceFactory != null && newCamera != null)
            {
                newCamera.CreateDeviceResources(graphicsDevice, resourceFactory);
            }
        }

        private void UpdateLightSystem(LightSystem? oldSystem, LightSystem? newSystem)
        {
            oldSystem?.DestroyDeviceResources();

            if (graphicsDevice != null && resourceFactory != null && newSystem != null)
            {
                newSystem.CreateDeviceResources(graphicsDevice, resourceFactory);
            }
        }

    }
}
