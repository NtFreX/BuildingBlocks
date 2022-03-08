namespace NtFreX.BuildingBlocks.Standard;

public interface IProtobufSerializable<TSerializable, TReal>
{
    TSerializable ToSerializable();
    static abstract TReal FromSerializable(TSerializable data);
}
