using Veldrid;

namespace NtFreX.BuildingBlocks.Standard
{
    // TODO: command list is already thread save? just use one list for same group? how to do grouping?
    public class CommandListPool : IDisposable
    {
        private readonly Dictionary<CommandListDescription, List<PooledCommandList>> pool = new Dictionary<CommandListDescription, List<PooledCommandList>>();
        private readonly ResourceFactory resourceFactory;

        public CommandListPool(ResourceFactory resourceFactory)
        {
            this.resourceFactory = resourceFactory;
        }


        private static CommandList CreateNewCommandList(ResourceFactory resourceFactory, ref CommandListDescription commandListDescription)
        {
            var cmdList = resourceFactory.CreateCommandList(ref commandListDescription);
            cmdList.Begin();
            return cmdList;
        }

        public static void TryClean(GraphicsDevice graphicsDevice, PooledCommandList commandList, CommandListPool? commandListPool = null)
        {
            if (commandListPool == null)
            {
                commandList.Item.End();
                graphicsDevice.SubmitCommands(commandList.Item);
                graphicsDevice.WaitForIdle();
                commandList.Item.Dispose();
            }
            else
            {
                commandListPool?.Free(commandList);
            }
        }

        public static PooledCommandList TryGet(ResourceFactory resourceFactory, CommandListPool? commandListPool = null)
        {
            var commandListDescription = new CommandListDescription();
            var pooledComandList = commandListPool == null ? null : commandListPool.Get(commandListDescription);
            return pooledComandList == null ? new PooledCommandList(CreateNewCommandList(resourceFactory, ref commandListDescription)) : pooledComandList;
        }

        public PooledCommandList Get(CommandListDescription description)
        {
            if (!pool.TryGetValue(description, out var cmdListPool))
            {
                cmdListPool = new List<PooledCommandList>();
                pool.Add(description, cmdListPool);
            }

            var cmdList = cmdListPool.FirstOrDefault(x => x.Free);
            if(cmdList != null)
            {
                cmdList.Free = false;
                return cmdList;
            }

            var newCmdList = new PooledCommandList(resourceFactory.CreateCommandList(description));
            newCmdList.Item.Begin();
            cmdListPool.Add(newCmdList);
            return newCmdList;
        }

        public void Free(PooledCommandList? commandList)
        {
            if(commandList != null)
                commandList.Free = true;
        }

        public void Submit(GraphicsDevice graphicsDevice)
        {
            foreach (var itemPool in pool.Values)
            {
                foreach (var value in itemPool)
                {
                    value.Item.End();
                    graphicsDevice.SubmitCommands(value.Item);
                    graphicsDevice.WaitForIdle();
                }
            }
        }

        public void Dispose()
        {
            foreach(var itemPool in pool.Values)
            {
                foreach(var value in itemPool)
                {
                    value.Item.Dispose();
                }
            }
        }
    }
}
