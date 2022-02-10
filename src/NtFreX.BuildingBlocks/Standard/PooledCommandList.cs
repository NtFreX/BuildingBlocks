using Veldrid;

namespace NtFreX.BuildingBlocks.Standard
{
    public class PooledCommandList
    {
        public CommandList Item { get; }
        public bool Free { get; set; }

        public PooledCommandList(CommandList item)
        {
            Item = item;
        }
    }
}
