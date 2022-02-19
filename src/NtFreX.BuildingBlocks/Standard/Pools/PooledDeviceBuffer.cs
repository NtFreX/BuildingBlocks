using Veldrid;

namespace NtFreX.BuildingBlocks.Standard.Pools
{
    public class PooledDeviceBuffer
    {
        public readonly DeviceBuffer RealDeviceBuffer;
        private readonly bool destroyDirectly;

        public event EventHandler? PoolReleased;

        public PooledDeviceBuffer(DeviceBuffer deviceBuffer, bool destroyDirectly = true)
        {
            this.RealDeviceBuffer = deviceBuffer;
            this.destroyDirectly = destroyDirectly;
        }

        public uint SizeInBytes => RealDeviceBuffer.SizeInBytes;
        public BufferUsage Usage => RealDeviceBuffer.Usage;
        public string Name { get => RealDeviceBuffer.Name; set => RealDeviceBuffer.Name = value; }
        public bool IsDisposed => RealDeviceBuffer.IsDisposed;
        public bool IsFree { get; private set; }

        public void Free()
        {
            if (destroyDirectly)
            {
                Destroy();
            }
            else
            {
                PoolReleased?.Invoke(this, EventArgs.Empty);
                IsFree = true;
            }
        }

        public void Use() => IsFree = false;

        public void Destroy()
        {
            RealDeviceBuffer.Dispose();
        }
    }
}
