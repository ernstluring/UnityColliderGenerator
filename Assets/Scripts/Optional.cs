
namespace Util
{
    /// <summary>
    /// A representation of an optional value, i.e. a value that may or may not be present.
    /// A common use case for optional is using it as the return value of a function that may fail.
    /// </summary>
    /// <typeparam name="T">The value type that can be optional</typeparam>
    public struct Optional<T>
    {
        public bool HasValue { get; private set; }
        private T value;
        public T Value
        {
            get
            {
                if (HasValue)
                    return value;
                else
                    throw new System.InvalidOperationException();
            }
        }

        public Optional(T value)
        {
            this.value = value;
            HasValue = value != null;
        }

        public static explicit operator T(Optional<T> optional)
        {
            return optional.Value;
        }
        public static implicit operator Optional<T>(T value)
        {
            return new Optional<T>(value);
        }

        public override bool Equals(object obj)
        {
            if (obj is Optional<T>)
                return this.Equals((Optional<T>)obj);
            else
                return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool Equals(Optional<T> other)
        {
            if (HasValue && other.HasValue)
                return object.Equals(value, other.value);
            else
                return HasValue == other.HasValue;
        }

        public static Optional<T> NullOpt()
        {
            return new Optional<T>();
        }
    }
}
