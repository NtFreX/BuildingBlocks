namespace NtFreX.BuildingBlocks.Standard
{
    public interface IMutable<T>
    {
        T Value { get; set; }
        event EventHandler<T>? ValueChanged;
    }
    public class Mutable<T> : IMutable<T>
    {
        private T value;
        private readonly object sender;

        public event EventHandler<T>? ValueChanged;

        public T Value 
        { 
            get => value;
            set
            {
                this.value = value ?? throw new ArgumentNullException();
                ValueChanged?.Invoke(sender, value);
            }
        }

        public Mutable(T value, object sender)
        {
            this.value = value;
            this.sender = sender;
        }

        public override string? ToString()
            => value?.ToString() ?? base.ToString();

        public static implicit operator T(Mutable<T> m) => m.Value;
    }

    public class MutableWrapper<T> : IMutable<T>
    {
        private readonly Func<T> getter;
        private readonly Action<T> setter;
        private readonly object sender;

        public T Value 
        { 
            get => getter(); 
            set
            { 
                setter(value);
                ValueChanged?.Invoke(sender, value);
            } 
        }

        public event EventHandler<T>? ValueChanged;

        public override string? ToString()
            => getter()?.ToString() ?? base.ToString();

        public MutableWrapper(Func<T> getter, Action<T> setter, object sender)
        {
            this.getter = getter;
            this.setter = setter;
            this.sender = sender;
        }
    }
}
