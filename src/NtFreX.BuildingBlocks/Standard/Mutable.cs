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

        public event EventHandler<T>? ValueChanged;

        public T Value 
        { 
            get => value;
            set
            {
                this.value = value ?? throw new ArgumentNullException();
                ValueChanged?.Invoke(this, value);
            }
        }

        public Mutable(T value)
        {
            this.value = value;
        }

        public override string? ToString()
            => value?.ToString() ?? base.ToString();

        public static implicit operator T(Mutable<T> m) => m.Value;
    }

    public class MutableWrapper<T> : IMutable<T>
    {
        private readonly Func<T> getter;
        private readonly Action<T> setter;

        public T Value 
        { 
            get => getter(); 
            set
            { 
                setter(value);
                ValueChanged?.Invoke(this, value);
            } 
        }

        public event EventHandler<T>? ValueChanged;

        public MutableWrapper(Func<T> getter, Action<T> setter)
        {
            this.getter = getter;
            this.setter = setter;
        }
    }
}
