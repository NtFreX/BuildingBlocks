using ProtoBuf;

namespace NtFreX.BuildingBlocks.Standard;

public static class ProtobufSerializableExtensions
{
    public static void WriteTo<TSerializable, TReal>(this IProtobufSerializable<TSerializable, TReal> serializable, string filePath, FileMode fileMode = FileMode.OpenOrCreate)
    {
        using var stream = File.Open(filePath, fileMode);
        WriteTo(serializable, stream);
    }

    public static void WriteTo<TSerializable, TReal>(this IProtobufSerializable<TSerializable, TReal> serializable, Stream stream)
        => Serializer.Serialize(stream, serializable.ToSerializable());

    public static TReal ReadFrom<TSerializable, TReal>(string filePath, FileMode fileMode = FileMode.Open) where TReal : IProtobufSerializable<TSerializable, TReal>
    {
        using var stream = File.Open(filePath, fileMode);
        return ReadFrom<TSerializable, TReal>(stream);
    }

    public static TReal ReadFrom<TSerializable, TReal>(Stream stream) where TReal : IProtobufSerializable<TSerializable, TReal>
        => TReal.FromSerializable(Serializer.Deserialize<TSerializable>(stream));
}