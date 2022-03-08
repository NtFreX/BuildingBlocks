using NtFreX.BuildingBlocks.Standard.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace NtFreX.BuildingBlocks.Standard
{
    public class ValueChangingEventArgs<T> : EventArgs
    {
        public T OldValue { get; }
        public T NewValue { get; }

        public ValueChangingEventArgs(T oldValue, T newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
    public interface IMutable<T>
    {
        T Value { get; set; }
        event EventHandler<T>? ValueChanged;
    }
    public class Mutable<T> : IMutable<T>
    {
        private T value;
        private readonly object sender;

        public event EventHandler<ValueChangingEventArgs<T>>? ValueChanging;
        public event EventHandler<T>? ValueChanged;

        public T Value 
        { 
            get => value;
            set
            {
                ValueChanging?.Invoke(sender, new ValueChangingEventArgs<T>(this.value, value));
                this.value = value;
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

        public static implicit operator T(Mutable<T> m) 
            => m.Value;

        public static bool operator !=(Mutable<T>? one, Mutable<T>? two)
            => !(one == two);

        public static bool operator ==(Mutable<T>? one, Mutable<T>? two)
            => EqualsExtensions.EqualsReferenceType(one, two);

        public override bool Equals([NotNullWhen(true)] object? obj)
            => EqualsExtensions.EqualsObject(this, obj);

        public bool Equals(Mutable<T>? other)
            => other?.Value?.Equals(Value) ?? false;

        public override int GetHashCode()
            => Value?.GetHashCode() ?? 0;
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
