using System.Runtime.InteropServices;

namespace ASCOM.LocalServer
{
    /// <summary>
    /// Base class for COM reference counting. Increments the server's global object count on
    /// construction and decrements it on finalization, triggering server shutdown when the count
    /// reaches zero.
    /// </summary>
    [ComVisible(false)]
    public class ReferenceCountedObjectBase
    {
        /// <summary>
        /// Called every time a driver instance is created
        /// </summary>
        public ReferenceCountedObjectBase()
        {
            // We increment the global count of objects.
            Server.IncrementObjectCount();
        }

        /// <summary>
        /// Called automatically by the garbage collection mechanic every time a driver instance is finalised (destroyed).
        /// </summary>
        ~ReferenceCountedObjectBase()
        {
            // We decrement the global count of objects.
            Server.DecrementObjectCount();

            // We then immediately test to see if we the conditions
            // are right to attempt to terminate this server application.
            Server.ExitIf();
        }
    }
}
