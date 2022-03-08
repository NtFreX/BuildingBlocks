using Veldrid;

namespace NtFreX.BuildingBlocks.Standard.Pools
{
    public class PooledCommandList
    {
        public CommandList CommandList { get; }

        public PooledCommandList(CommandList item)
        {
            CommandList = item;
        }
    }
}
