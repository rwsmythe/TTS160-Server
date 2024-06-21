using JetBrains.Annotations;
using System.Threading;

namespace ASCOM.TTS160
{
    public class ThreadSafeValue<T>
    {
        private object _value;

        public ThreadSafeValue(object value) => _value = value;

        public void Set(in T value) => Interlocked.Exchange(ref _value, value);

        public static implicit operator ThreadSafeValue<T>(in T value) => new ThreadSafeValue<T>(value);

        public static implicit operator T([NotNull] ThreadSafeValue<T> @this) => (T)(@this?._value ?? default);

    }
}
