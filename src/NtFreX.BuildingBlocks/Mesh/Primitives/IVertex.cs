using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh.Primitives;

public interface IVertex
{
    public static abstract ushort BytesBeforePosition { get; }
    public static abstract VertexLayoutDescription VertexLayout { get; }
}
