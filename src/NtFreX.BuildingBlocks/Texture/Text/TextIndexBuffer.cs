using NtFreX.BuildingBlocks.Standard.Extensions;
using NtFreX.BuildingBlocks.Standard.Pools;
using System.Runtime.InteropServices;
using Veldrid;

namespace NtFreX.BuildingBlocks.Texture.Text;

internal class TextIndexBuffer
{
    private const uint MaxTextLengthSizeIncrease = 1000;
    private uint MaxTextLength = 5000;

    public PooledDeviceBuffer? IndexBuffer { get; private set; }

    public static TextIndexBuffer Instance { get; } = new TextIndexBuffer();

    private TextIndexBuffer() { }

    public void DestroyDeviceResources()
    {
        IndexBuffer?.Destroy();
        IndexBuffer = null;
    }

    public void BuildIndexBuffer(ResourceFactory resourceFactory, CommandList commandList, int textLength, DeviceBufferPool? deviceBufferPool = null)
    {
        // TODO: create new model part instead of resizing buffer?
        if (IndexBuffer != null && MaxTextLength < textLength)
        {
            IndexBuffer.Destroy();
            IndexBuffer = null;
            while (MaxTextLength < textLength)
            {
                MaxTextLength += MaxTextLengthSizeIncrease;
                // we need 6 indices per character, the index format is 16 bits (ushort)
                if (MaxTextLength * 6 > ushort.MaxValue)
                    throw new Exception($"Only texts with a max length of {ushort.MaxValue / 6} are supported");
            }
        }
        if (IndexBuffer == null)
        {
            var desc = new BufferDescription((uint)(MaxTextLength * 6 * Marshal.SizeOf(typeof(ushort))), BufferUsage.IndexBuffer);
            IndexBuffer = resourceFactory.CreatedPooledBuffer(desc, "text_indexbuffer", deviceBufferPool);
            commandList.UpdateBuffer(IndexBuffer.RealDeviceBuffer, 0, BuildIndices());
        }
    }

    private ushort[] BuildIndices()
    {
        var indices = new ushort[MaxTextLength * 6];
        for (var i = 0; i < MaxTextLength; i++)
        {
            indices[i * 6] = (ushort)(i * 4 + 1);
            indices[i * 6 + 1] = (ushort)(i * 4 + 2);
            indices[i * 6 + 2] = (ushort)(i * 4);

            indices[i * 6 + 3] = (ushort)(i * 4 + 2);
            indices[i * 6 + 4] = (ushort)(i * 4 + 1);
            indices[i * 6 + 5] = (ushort)(i * 4 + 3);
        }
        return indices;
    }
}
