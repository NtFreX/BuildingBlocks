namespace NtFreX.BuildingBlocks.Standard
{
    public class Cached<T>
    {
        private readonly object locker = new ();
        private readonly Func<bool>? allwaysInvalidWhen;
        private readonly Func<T> getter;

        private bool initialized;
        private T? value;

        public Cached(Func<T> getter, Func<bool>? allwaysInvalidWhen = null)
        {
            this.allwaysInvalidWhen = allwaysInvalidWhen;
            this.getter = getter;
        }

        public void Invalidate()
            => initialized = false;

        public T Get()
        {
            lock (locker)
            {
                if (!initialized || (allwaysInvalidWhen?.Invoke() ?? false) || value == null)
                {
                    value = getter();
                    initialized = true;
                }
                return value;
            }
        }
    }
}
