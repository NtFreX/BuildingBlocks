using NtFreX.BuildingBlocks.Input;
using NtFreX.BuildingBlocks.Mesh.Data;
using NtFreX.BuildingBlocks.Mesh.Data.Specialization;
using NtFreX.BuildingBlocks.Mesh.Factories;
using NtFreX.BuildingBlocks.Model;
using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Extensions;
using NtFreX.BuildingBlocks.Standard.Pools;
using System.Diagnostics;
using System.Numerics;
using Veldrid;
using Veldrid.Utilities;


namespace NtFreX.BuildingBlocks.Mesh
{
    //TODO: wrap with model which creates device resources and contains multiple meshes (bone transforms?)
    public class MeshRenderer : CullRenderable, IUpdateable
    {
        // TODO: make configurable
        private static MeshRenderPass[] MeshRenderPasses => MeshRenderPassFactory.RenderPasses.ToArray();


        private readonly List<MeshRenderPass> meshRenderPassesWorkList = new ();
        private readonly Cached<Vector3> centerCache;
        private readonly DeviceBufferPool? deviceBufferPool;
        private readonly CommandListPool? commandListPool;
        private readonly MeshBufferBuilder meshBufferBuilder;

        private BoundingBox boundingBox;
        private BoundingBox meshBoundingBox;
        private bool hasWorldChanged = true;
        private bool hasMeshRenderPassDataChanged = true; // TODO: initialise render passes in costructor and make it up to the user to update them?!
        private int meshRenderPassHashCode;
        private RenderPasses renderPasses;

        public readonly Mutable<bool> IsActive;
        public readonly IMutable<Transform> Transform;
        public readonly SpecializedMeshData MeshData;

        public uint IndexCount { get; private set; }
        public PooledDeviceBuffer? VertexBuffer { get; private set; }
        public ResourceSet? WorldResourceSet { get; private set; }
        public ResourceSet? InverseWorldResourceSet { get; private set; }
        public PooledDeviceBuffer? WorldBuffer { get; private set; }
        public PooledDeviceBuffer? InverseWorldBuffer { get; private set; }
        public Matrix4x4? WorldMatrix { get; private set; }
        public PooledDeviceBuffer? IndexBuffer { get; private set; }
        public MeshRenderPass[] CurrentMeshRenderPasses { get; private set; }

        public override BoundingBox GetBoundingBox() => boundingBox; //  TODO: rename frustum culling bounding box and seperate the buffer
        public override Vector3 GetCenter() => centerCache.Get();
        public override RenderPasses RenderPasses => renderPasses;

        // TODO: give possibilities to customize render passes
        public static async Task<MeshRenderer> CreateAsync(MeshDataProvider meshDataProvider, Transform? transform = null, string? name = null, bool isActive = true, DeviceBufferPool? deviceBufferPool = null, CommandListPool? commandListPool = null, MeshBufferBuilder? meshBufferBuilder = null)
        {
            (var meshData, var meshBoundingBox) = await meshDataProvider.GetAsync();
            return new MeshRenderer(meshData, meshBoundingBox, transform, name, isActive, deviceBufferPool, commandListPool, meshBufferBuilder);
        }

        private MeshRenderer(SpecializedMeshData specializedMeshData, BoundingBox boundingBox, Transform? transform = null, string? name = null, bool isActive = true, DeviceBufferPool? deviceBufferPool = null, CommandListPool? commandListPool = null, MeshBufferBuilder? meshBufferBuilder = null)
        {
            this.deviceBufferPool = deviceBufferPool;
            this.commandListPool = commandListPool;
            this.meshBufferBuilder = meshBufferBuilder ?? new DefaultMeshBufferBuilder();

            var realTransform = transform ?? new Transform();
            MeshData = specializedMeshData;
            IsActive = new Mutable<bool>(isActive, this);
            Transform = new Mutable<Transform>(new Transform(realTransform.Position, realTransform.Rotation, realTransform.Scale), this);
            CurrentMeshRenderPasses = Array.Empty<MeshRenderPass>();
            Name = name;

            IsActive.ValueChanged += (_, _) => UpdateShouldRender();
            Transform.ValueChanged += (_, _) => InvalidateWorldCache();

            meshBoundingBox = boundingBox;
            centerCache = new Cached<Vector3>(() => GetBoundingBox().GetCenter());

            InvalidateWorldCache();
            UpdateShouldRender();
            UpdateRenderPasses();
        }

        private void UpdateRenderPasses()
        {
            //TODO: decouple
            var defaultRenderPasses = RenderPasses.Geometry | RenderPasses.Forward | RenderPasses.AllShadowMap;
            var alphaRenderPasses = RenderPasses.GeometryAlpha | RenderPasses.AlphaBlend | RenderPasses.AllShadowMap;
            if (MeshData.Specializations.TryGet<PhongMaterialMeshDataSpecialization>(out var specialization))
            {
                renderPasses = specialization.Material.Value.Opacity == 1f ? defaultRenderPasses : alphaRenderPasses;
            }
            else
            {
                renderPasses = defaultRenderPasses;
            }
        }

        private void UpdateBoundingBox()
        {
            // TODO: apply bone transforms!!

            BoundingBox? instanceBoundingBox = null;
            if (MeshData.Specializations.TryGet<InstancedMeshDataSpecialization>(out var instancedMeshDataSpecialization))
            {
                foreach (var instance in instancedMeshDataSpecialization.Instances.Value)
                {
                    var worldMatrix = new Transform(instance.Position, Matrix4x4.CreateRotationX(instance.Rotation.X) * Matrix4x4.CreateRotationY(instance.Rotation.Y) * Matrix4x4.CreateRotationZ(instance.Rotation.Z), instance.Scale).CreateWorldMatrix();
                    var box = BoundingBox.Transform(meshBoundingBox, worldMatrix);
                    instanceBoundingBox = instanceBoundingBox == null ? box : BoundingBox.Combine(instanceBoundingBox.Value, box);
                }
            }

            BoundingBox newBoundingBox = instanceBoundingBox == null ? meshBoundingBox : instanceBoundingBox.Value;
            newBoundingBox = newBoundingBox.TransformBoundingBox(Transform.Value);

            Debug.Assert(!newBoundingBox.ContainsNaN(), "The new bounding box should never contain NaN");
            if (boundingBox.ContainsNotOrIsNotSame(newBoundingBox))
            {
                boundingBox = new BoundingBox(newBoundingBox.Min - BoundingBoxSpacer, newBoundingBox.Max + BoundingBoxSpacer);
                centerCache.Invalidate();
                PublishNewBoundingBoxAvailable();
            }
        }

        private void InvalidateWorldCache()
        {
            hasMeshRenderPassDataChanged = true;

            var newWorldMatrix = Transform.Value.CreateWorldMatrix();

            Debug.Assert(!newWorldMatrix.ContainsNaN(), "The new world matrix should never contain NaN");
            if (newWorldMatrix != WorldMatrix)
            {
                WorldMatrix = newWorldMatrix;
                UpdateBoundingBox();
                hasWorldChanged = true;
            }
        }

        private void UpdateShouldRender()
            => ShouldRender = IsActive && (!MeshData.Specializations.TryGet<PhongMaterialMeshDataSpecialization>(out var specialization) || specialization?.Material == null || specialization.Material.Value.Opacity > 0f);

        private void SpecializationChanged(object? sender, Type e)
        {
            hasMeshRenderPassDataChanged = true;

            //TODO: move to individuell specializations
            if(e == typeof(PhongMaterialMeshDataSpecialization))
            {
                UpdateShouldRender();
                UpdateRenderPasses();

                // todo: remove event from previous value!!
                MeshData.Specializations.Get<PhongMaterialMeshDataSpecialization>().Material.ValueChanged += MaterialChanged;
            }
            else if (e == typeof(InstancedMeshDataSpecialization))
            {
                UpdateBoundingBox();

                // todo: remove event from previous value!!
                MeshData.Specializations.Get<InstancedMeshDataSpecialization>().Instances.ValueChanged += InstancesChanged;
            }
        }

        private void MaterialChanged(object? sender, Data.Specialization.Primitives.PhongMaterialInfo e)
        {
            UpdateShouldRender();
            UpdateRenderPasses();
        }

        private void InstancesChanged(object? sender, Data.Specialization.Primitives.InstanceInfo[] e)
        {
            UpdateBoundingBox();
        }

        public void Update(float deltaSeconds, InputHandler inputHandler)
        {
            // lazy buffers for draw calls
            if (hasWorldChanged && WorldMatrix != null && CurrentGraphicsDevice != null)
            {
                Debug.Assert(WorldBuffer != null);
                Debug.Assert(InverseWorldBuffer != null);

                CurrentGraphicsDevice.UpdateBuffer(WorldBuffer.RealDeviceBuffer, 0, WorldMatrix.Value);

                Matrix4x4 inverted;
                Matrix4x4.Invert(WorldMatrix.Value, out inverted);
                CurrentGraphicsDevice.UpdateBuffer(InverseWorldBuffer.RealDeviceBuffer, 0, Matrix4x4.Transpose(inverted));
                hasWorldChanged = false;
            }

            if (hasMeshRenderPassDataChanged)
            {
                meshRenderPassesWorkList.Clear();
                foreach (var meshRednerPass in MeshRenderPasses)
                {
                    if (meshRednerPass.CanBindMeshRenderer(this))
                    {
                        meshRenderPassesWorkList.Add(meshRednerPass);
                    }
                }
                CurrentMeshRenderPasses = meshRenderPassesWorkList.ToArray();
                meshRenderPassHashCode = HashCode.Combine(MeshData.Specializations.GetHashCode(), CurrentMeshRenderPasses?.FirstOrDefault()?.GetHashCode() ?? 0);
                hasMeshRenderPassDataChanged = false;
            }

            // TODO: move this to the BonesMeshDataSpecialization type
            if (MeshData.Specializations.TryGet<BonesMeshDataSpecialization>(out var bonesSpecialization))
            {
                foreach (var boneAnimation in bonesSpecialization.BoneAnimationProviders ?? Array.Empty<IBoneAnimationProvider>())
                {
                    if (boneAnimation.IsRunning)
                    {
                        var transforms = bonesSpecialization.BoneTransforms.Value;
                        boneAnimation.UpdateAnimation(deltaSeconds, ref transforms);
                        bonesSpecialization.BoneTransforms.Value = transforms;
                        break;
                    }
                }
            }
        }

        //TODO: remove graphics device param?
        public override void Render(GraphicsDevice graphicsDevice, CommandList commandList, RenderContext renderContext, RenderPasses renderPass)
        {
            Debug.Assert(RenderPasses.HasFlag(renderPass));
            Debug.Assert(CurrentResourceFactory != null);
            Debug.Assert(CurrentScene != null);

            //TODO: do this in update device res
            if (MeshData.HasVertexChanges)
            {
                Debug.Assert(VertexBuffer != null);
                MeshData.UpdateVertexBuffer(commandList, VertexBuffer);
            }
            if (MeshData.HasIndexChanges)
            {
                Debug.Assert(IndexBuffer != null);
                MeshData.UpdateIndexBuffer(commandList, IndexBuffer);
            }

            foreach (var meshRednerPass in CurrentMeshRenderPasses)
            {
                if (meshRednerPass.CanBindRenderPass(renderPass))
                {
                    meshRednerPass.Bind(graphicsDevice, CurrentResourceFactory, this, renderContext, CurrentScene, commandList);
                    meshRednerPass.Draw(this, commandList);
                }
            }
        }

        public override RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
        {
            Debug.Assert(CurrentScene?.Camera.Value != null);

            return RenderOrderKey.Create(
                    meshRenderPassHashCode,
                    Vector3.Distance(GetCenter(), cameraPosition),
                    CurrentScene.Camera.Value.FarDistance);
        }

        public override async Task<bool> CreateDeviceObjectsAsync(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, CommandList commandList, RenderContext renderContext, Scene scene)
        {
            if (!await base.CreateDeviceObjectsAsync(graphicsDevice, resourceFactory, commandList, renderContext, scene))
                return false;

            Debug.Assert(CurrentResourceFactory != null);
            Debug.Assert(CurrentGraphicsDevice != null);
            Debug.Assert(CurrentScene != null);

            //TODO: use render context for pipeline output!
            var worldLayout = ResourceLayoutFactory.GetWorldLayout(CurrentResourceFactory);
            WorldBuffer = CurrentResourceFactory.CreatedPooledBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic), "meshRenderer_worldBuffer_" + Name, deviceBufferPool);
            WorldResourceSet = ResourceSetFactory.GetResourceSet(CurrentResourceFactory, new ResourceSetDescription(worldLayout, WorldBuffer.RealDeviceBuffer), "meshRenderer_worldResourceSet_" + Name);

            var inverseWorldLayout = ResourceLayoutFactory.GetInverseWorldLayout(resourceFactory);
            InverseWorldBuffer = CurrentResourceFactory.CreatedPooledBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic), "meshRenderer_inverseworldBuffer_" + Name, deviceBufferPool);
            InverseWorldResourceSet = ResourceSetFactory.GetResourceSet(CurrentResourceFactory, new ResourceSetDescription(inverseWorldLayout, InverseWorldBuffer.RealDeviceBuffer), "meshRenderer_inverseworldResourceSet_" + Name);

            hasWorldChanged = true;

            (VertexBuffer, IndexBuffer, IndexCount) = meshBufferBuilder.Build(CurrentGraphicsDevice, CurrentResourceFactory, MeshData, deviceBufferPool, commandListPool);

            foreach(var specialization in MeshData.Specializations)
            {
                await specialization.CreateDeviceObjectsAsync(CurrentGraphicsDevice, CurrentResourceFactory);
            }

            hasMeshRenderPassDataChanged = true;
            //TODO: update hasMeshRenderPassDataChanged! (do all this in ctor)
            //TODO: updateable stuff!!
            //TODO call CreateDeviceObjectsAsync when specialization changed and destroy!!
            //MeshDataProvider.Specializations.SpecializationChanged += (sender, key) => 
            MeshData.Specializations.SpecializationChanged += SpecializationChanged;
            if (MeshData.Specializations.TryGet<PhongMaterialMeshDataSpecialization>(out var phongMaterialMeshDataSpecialization))
                phongMaterialMeshDataSpecialization.Material.ValueChanged += MaterialChanged;
            if (MeshData.Specializations.TryGet<InstancedMeshDataSpecialization>(out var instancedMeshDataSpecialization))
                instancedMeshDataSpecialization.Instances.ValueChanged += InstancesChanged;
            //MeshDataProvider

            //scene.Camera.ValueChanged += (_, _) => UpdateProjectionViewWorldResourceSet();

            //this.MeshBuffer.Material.ValueChanged += (_, _) => UpdateShouldRender();  && UpdateRenderPasses();
            //this.MeshBuffer.InstanceBoundingBox.ValueChanged += (_, _) => UpdateBoundingBox();

            Update(0f, InputHandler.Empty); // TODO: this line is only nessesary because logic update and device resource update are combined (seperate it! and only load the device resoruces)
            return true;
        }

        public override void DestroyDeviceObjects()
        {
            Debug.Assert(CurrentScene != null);

            MeshData.Specializations.SpecializationChanged -= SpecializationChanged;
            if(MeshData.Specializations.TryGet<PhongMaterialMeshDataSpecialization>(out var phongMaterialMeshDataSpecialization)) 
                phongMaterialMeshDataSpecialization.Material.ValueChanged -= MaterialChanged;
            if (MeshData.Specializations.TryGet<InstancedMeshDataSpecialization>(out var instancedMeshDataSpecialization))
                instancedMeshDataSpecialization.Instances.ValueChanged -= InstancesChanged;

            base.DestroyDeviceObjects();

            VertexBuffer?.Destroy();
            VertexBuffer = null;

            IndexBuffer?.Destroy();
            IndexBuffer = null;

            WorldBuffer?.Destroy();
            WorldBuffer = null;

            InverseWorldBuffer?.Destroy();
            InverseWorldBuffer = null;

            WorldResourceSet?.Dispose();
            WorldResourceSet = null;

            InverseWorldResourceSet?.Dispose();
            InverseWorldResourceSet = null;

            foreach (var specialization in MeshData.Specializations)
            {
                specialization.DestroyDeviceObjects();
            }
            // TODO: cleanup resource sets from time to time?
        }
    }
}
