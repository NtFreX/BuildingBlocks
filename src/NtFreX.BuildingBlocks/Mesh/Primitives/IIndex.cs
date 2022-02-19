using Veldrid;

namespace NtFreX.BuildingBlocks.Mesh.Primitives;

public interface IIndex
{ 
    public static abstract IndexFormat IndexFormat { get; }
    public uint AsUInt();
}

public interface IIndex<TSelf> : IIndex
{
    public static abstract TSelf Parse(Index32 value);
    public static abstract TSelf Parse(Index16 value);
    public static abstract TSelf ParseShort(ushort value);
    public static abstract TSelf ParseInt(uint value);
}
