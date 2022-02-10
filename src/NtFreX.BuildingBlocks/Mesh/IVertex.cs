using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh;

public interface IVertex
{
    public static abstract ushort BytesBeforePosition { get; }
    public static abstract VertexLayoutDescription VertexLayout { get; }
}
