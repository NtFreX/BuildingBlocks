using Veldrid;

namespace NtFreX.BuildingBlocks.Standard.Pools
{
    //TODO: clean up this shit
    // TODO: command list is already thread save? just use one list for same group? how to do grouping?
    public sealed class CommandListPool : IDisposable
    {
        private readonly Dictionary<CommandListDescription, List<PooledCommandList>> pool = new ();
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

        public static void TrySubmit(GraphicsDevice graphicsDevice, PooledCommandList commandList, CommandListPool? commandListPool = null)
        {
            commandList.CommandList.End();
            graphicsDevice.SubmitCommands(commandList.CommandList);
        }

        public static PooledCommandList TryGet(ResourceFactory resourceFactory, CommandListDescription? description = null, CommandListPool? commandListPool = null)
        {
            var commandListDescription = description ?? new CommandListDescription();
            return new PooledCommandList(CreateNewCommandList(resourceFactory, ref commandListDescription));

            //var pooledComandList = commandListPool?.Get(commandListDescription);
            //return pooledComandList ?? new PooledCommandList(CreateNewCommandList(resourceFactory, ref commandListDescription));
        }

        private PooledCommandList Get(CommandListDescription description)
        {
            if (!pool.TryGetValue(description, out var cmdListPool))
            {
                cmdListPool = new List<PooledCommandList>();
                pool.Add(description, cmdListPool);
            }

            var cmdList = cmdListPool.FirstOrDefault();
            if(cmdList != null)
            {
                return cmdList;
            }

            var newCmdList = new PooledCommandList(CreateNewCommandList(resourceFactory, ref description));
            cmdListPool.Add(newCmdList);
            return newCmdList;
        }

        //public async Task SubmitAsync(GraphicsDevice graphicsDevice)
        //{
        //    //TODO: thread safty!!!
        //    submitTaskList.Clear();
        //    foreach (var itemPool in pool.Values)
        //    {
        //        foreach (var value in itemPool)
        //        {
        //            submitTaskList.Add(Task.Run(() =>
        //            {
        //                value.Item.End();
        //                graphicsDevice.SubmitCommands(value.Item);
        //                //graphicsDevice.WaitForIdle();
        //                value.Dispose();
        //                value.Free = true;
        //            }));
        //        }
        //    }
        //    await Task.WhenAll(submitTaskList);
        //}

        public void Dispose()
        {
            foreach (var itemPool in pool.Values)
            {
                foreach (var value in itemPool)
                {
                    value.CommandList.Dispose();
                }
            }
            pool.Clear();
        }

        //public static void Free(PooledCommandList? commandList)
        //{
        //    if (commandList != null)
        //    {
        //        commandList.Free = true;
        //    }
        //}
    }
}
