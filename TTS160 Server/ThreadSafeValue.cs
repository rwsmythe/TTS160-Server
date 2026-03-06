using JetBrains.Annotations;
using System.Threading;

namespace ASCOM.TTS160
{
    /// <summary>
    /// Generic thread-safe value wrapper using <see cref="Interlocked.Exchange(ref object, object)"/>
    /// for lock-free atomic reads and writes. Values are boxed to <see cref="object"/> to leverage
    /// the atomic reference-swap guarantee of Interlocked.Exchange.
    /// </summary>
    /// <remarks>
    /// <para>Implicit conversion operators allow transparent assignment and reading:
    /// <c>ThreadSafeValue&lt;bool&gt; v = true;</c> and <c>bool b = v;</c>.</para>
    /// <para>Used extensively by <see cref="MiscResources"/> to provide thread-safe state
    /// without explicit locking.</para>
    /// </remarks>
    /// <typeparam name="T">The type of value to wrap.</typeparam>
    public class ThreadSafeValue<T>
    {
        private object _value;

        public ThreadSafeValue(object value) => _value = value;

        public void Set(in T value) => Interlocked.Exchange(ref _value, value);

        public static implicit operator ThreadSafeValue<T>(in T value) => new ThreadSafeValue<T>(value);

        public static implicit operator T([NotNull] ThreadSafeValue<T> @this) => (T)(@this?._value ?? default);

    }
}
