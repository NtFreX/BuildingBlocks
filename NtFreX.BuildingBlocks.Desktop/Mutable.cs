using System;

namespace NtFreX.BuildingBlocks.Desktop
{
    public class Mutable<T>
    {
        private T value;

        public event EventHandler<T> ValueChanged;

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
    }
}
