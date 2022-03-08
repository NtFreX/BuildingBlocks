using NtFreX.BuildingBlocks.Mesh.Data.Specialization.Primitives;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh.Factories
{
    public static class VertexLayoutFactory
    {
        public static VertexLayoutDescription[] CreateDefaultLayout(VertexLayoutDescription vertex, bool hasBones, bool hasInstances)
        {
            var vertexLayoutDescription = new List<VertexLayoutDescription> { vertex };
            if (hasBones)
                vertexLayoutDescription.Add(new VertexLayoutDescription(
                    new VertexElementDescription("BoneWeights", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4),
                    new VertexElementDescription("BoneIndices", VertexElementSemantic.TextureCoordinate, VertexElementFormat.UInt4)));
            if (hasInstances)
                vertexLayoutDescription.Add(InstanceInfo.VertexLayout);
            return vertexLayoutDescription.ToArray();
        }
    }
}
