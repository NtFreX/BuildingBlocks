using NtFreX.BuildingBlocks.Model;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;

namespace NtFreX.BuildingBlocks
{
    public class Scene
    {
        private readonly Octree<Renderable> frustumTree = new Octree<Renderable>(new BoundingBox(new Vector3(float.MinValue), new Vector3(float.MaxValue)), 2);
        private readonly HashSet<Renderable> freeRenderables = new HashSet<Renderable>();
        private readonly HashSet<CullRenderable> cullRenderables = new HashSet<CullRenderable>();
        private readonly HashSet<IUpdateable> updateables = new HashSet<IUpdateable>();

        public IUpdateable[] Updateables => updateables.ToArray();
        public CullRenderable[] CullRenderables => cullRenderables.ToArray();

        public void GetContainedRenderables(BoundingFrustum frustum, List<Renderable> renderables)
        {
            frustumTree.GetContainedObjects(frustum, renderables);
            renderables.AddRange(freeRenderables);
        }

        internal void DestroyAllDeviceObjects()
        {
            foreach (CullRenderable cr in cullRenderables)
            {
                cr.DestroyDeviceObjects();
            }
            foreach (Renderable r in freeRenderables)
            {
                r.DestroyDeviceObjects();
            }
        }

        internal void CreateAllDeviceObjects(GraphicsDevice gd, CommandList cl, RenderContext rc)
        {
            foreach (CullRenderable cr in cullRenderables)
            {
                cr.CreateDeviceObjects(gd, cl, rc);
            }
            foreach (Renderable r in freeRenderables)
            {
                r.CreateDeviceObjects(gd, cl, rc);
            }
        }

        public void AddUpdateables(params IUpdateable[] models)
        {
            foreach (var updateable in models)
            {
                if (updateables.Contains(updateable))
                    continue;

                updateables.Add(updateable);
            }
        }

        public void RemoveUpdateables(params IUpdateable[] models)
        {
            foreach (var updateable in models)
            {
                if (!updateables.Contains(updateable))
                    continue;

                updateables.Remove(updateable);
            }
        }

        public void AddFreeRenderables(params Renderable[] models)
        {
            foreach (var model in models)
            {
                if (freeRenderables.Contains(model))
                    continue;

                model.ShouldRenderHasChanged += UpdateShouldRender;
                if (model.ShouldRender)
                    AddFreeRenderableCore(model);
            }
        }

        public void RemoveFreeRenderables(params Renderable[] models)
        {
            foreach (var model in models)
            {
                if (!freeRenderables.Contains(model))
                    continue;

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

                model.ShouldRenderHasChanged -= UpdateCullableShouldRender;
                RemoveCullRenderableCore(model);
            }
        }

        public void AddCullRenderables(params CullRenderable[] models)
        {
            foreach (var model in models)
            {
                if (cullRenderables.Contains(model))
                    continue;

                model.ShouldRenderHasChanged += UpdateCullableShouldRender;
                if (model.ShouldRender)
                    AddCullRenderableCore(model);
            }
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
    }
}
