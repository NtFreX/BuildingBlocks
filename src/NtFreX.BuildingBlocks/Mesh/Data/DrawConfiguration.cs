using NtFreX.BuildingBlocks.Standard;
using NtFreX.BuildingBlocks.Standard.Extensions;
using ProtoBuf;
using System.Diagnostics.CodeAnalysis;
using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh.Data;

public class DrawConfiguration : IProtobufSerializable<DrawConfiguration.Protobuf, DrawConfiguration>
{
    [ProtoContract]
    public class VertexElementDescriptionProtobuf
    {
        [ProtoMember(100)] public string Name;
        [ProtoMember(101)] public VertexElementSemantic Semantic;
        [ProtoMember(102)] public VertexElementFormat Format;
        [ProtoMember(103)] public uint Offset;
    }

    [ProtoContract]
    public class VertexLayoutDescriptionProtobuf
    {
        [ProtoMember(201)] public uint Stride;
        [ProtoMember(202)] public VertexElementDescriptionProtobuf[] Elements;
        [ProtoMember(203)] public uint InstanceStepRate;
    }

    [ProtoContract]
    public class Protobuf
    {
        [ProtoMember(300)] public IndexFormat IndexFormat;
        [ProtoMember(301)] public VertexLayoutDescriptionProtobuf VertexLayout;
        [ProtoMember(302)] public PrimitiveTopology PrimitiveTopology;
        [ProtoMember(303)] public PolygonFillMode FillMode;
        [ProtoMember(304)] public FaceCullMode FaceCullMode;
    }

    private static VertexElementDescriptionProtobuf ToSerializable(VertexElementDescription real) => new VertexElementDescriptionProtobuf { Format = real.Format, Name = real.Name, Offset = real.Offset, Semantic = real.Semantic };
    private static VertexLayoutDescriptionProtobuf ToSerializable(VertexLayoutDescription real) => new VertexLayoutDescriptionProtobuf { Stride = real.Stride, InstanceStepRate = real.InstanceStepRate, Elements = real.Elements.Select(ToSerializable).ToArray() };
    private static VertexElementDescription FromSerializable(VertexElementDescriptionProtobuf serializable) => new VertexElementDescription(serializable.Name, serializable.Semantic, serializable.Format, serializable.Offset);
    private static VertexLayoutDescription FromSerializable(VertexLayoutDescriptionProtobuf serializable) => new VertexLayoutDescription(serializable.Stride, serializable.InstanceStepRate, serializable.Elements.Select(FromSerializable).ToArray());

    public Protobuf ToSerializable() => new Protobuf { IndexFormat = IndexFormat, VertexLayout = ToSerializable(VertexLayout), PrimitiveTopology = PrimitiveTopology, FaceCullMode = FaceCullMode };
    public static DrawConfiguration FromSerializable(Protobuf data) => new DrawConfiguration(data.IndexFormat, data.PrimitiveTopology, FromSerializable(data.VertexLayout), data.FillMode, data.FaceCullMode);

    public IndexFormat IndexFormat { get; }
    public VertexLayoutDescription VertexLayout { get; }
    public Mutable<PrimitiveTopology> PrimitiveTopology { get; }
    public Mutable<PolygonFillMode> FillMode { get; }
    public Mutable<FaceCullMode> FaceCullMode { get; }

    public DrawConfiguration(IndexFormat indexFormat, PrimitiveTopology primitiveTopology, VertexLayoutDescription vertexLayout, PolygonFillMode fillMode, FaceCullMode faceCullMode)
    {
        IndexFormat = indexFormat;
        VertexLayout = vertexLayout;
        PrimitiveTopology = new Mutable<PrimitiveTopology>(primitiveTopology, this);
        FillMode = new Mutable<PolygonFillMode>(fillMode, this);
        FaceCullMode = new Mutable<FaceCullMode>(faceCullMode, this);
    }

    public static bool operator !=(DrawConfiguration? one, DrawConfiguration? two)
        => !(one == two);

    public static bool operator ==(DrawConfiguration? one, DrawConfiguration? two)
        => EqualsExtensions.EqualsReferenceType(one, two);

    public override int GetHashCode()
        => (IndexFormat, VertexLayout, PrimitiveTopology, FillMode, FaceCullMode).GetHashCode();

    public override string ToString()
        => $"IndexFormat: {IndexFormat}, VertexLayout: {VertexLayout}, PrimitiveTopology: {PrimitiveTopology}, FillMode: {FillMode}, FaceCullMode: {FaceCullMode}";

    public override bool Equals([NotNullWhen(true)] object? obj)
        => EqualsExtensions.EqualsObject(this, obj);

    public bool Equals(DrawConfiguration? other)
    {
        if (other == null)
            return false;
        if(other.IndexFormat != IndexFormat)
            return false;
        if (!other.VertexLayout.Equals( VertexLayout))
            return false;
        if (other.PrimitiveTopology != PrimitiveTopology)
            return false;
        if (other.FillMode != FillMode)
            return false;
        if (other.FaceCullMode != FaceCullMode)
            return false;
        return true;
    }
}
