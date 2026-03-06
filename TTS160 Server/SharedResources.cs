//
// ================
// Shared Resources
// ================
//
// This class is a container for all shared resources that may be needed
// by the drivers served by the Local Server. 
//
// NOTES:
//
//	* ALL DECLARATIONS MUST BE STATIC HERE!! INSTANCES OF THIS CLASS MUST NEVER BE CREATED!

using ASCOM.Utilities;
using ASCOM.TTS160.Telescope;
using System.Threading;
using System.Diagnostics;
using System;

namespace ASCOM.LocalServer
{
    /// <summary>
    /// Static container for resources shared across all driver instances served by this COM local server.
    /// Manages the single shared serial port connection to the TTS-160 mount, providing thread-safe
    /// message sending, connection reference counting, and buffer management.
    /// </summary>
    /// <remarks>
    /// <para>All declarations are static — instances of this class must never be created.</para>
    /// <para>Decorated with <see cref="HardwareClassAttribute"/> so its <see cref="Dispose"/> method
    /// is called automatically when the local server shuts down.</para>
    /// <para>The serial port is configured for 9600 baud, 8N1 with a 500ms receive timeout.</para>
    /// </remarks>
    [HardwareClass]
    public static class SharedResources
    {
        // Object used for locking to prevent multiple drivers accessing common code at the same time
        private static readonly object lockObject = new object();

        // Shared serial port. This will allow multiple drivers to use one single serial port.
        private static readonly Serial sharedSerial = new Serial();      // Shared serial port
        private static int serialConnectionCount = 0;     // counter for the number of connections to the serial port
        private static readonly int RECEIVETIMEOUT = 500;  //Reduce serial timeout to 0.5 seconds from 5.
        private static readonly Stopwatch stopwatch = new Stopwatch();
        // Public access to shared resources

        #region Dispose method to clean up resources before close
        /// <summary>
        /// Deterministically release both managed and unmanaged resources that are used by this class.
        /// </summary>
        /// <remarks>
        /// TODO: Release any managed or unmanaged resources that are used in this class.
        /// 
        /// Do not call this method from the TelescopeHardware.Dispose() method in your hardware class.
        ///
        /// This is because this shared resources class is decorated with the <see cref="HardwareClassAttribute"/> attribute and this Dispose() method will be called 
        /// automatically by the local server executable when it is irretrievably shutting down. This gives you the opportunity to release managed and unmanaged resources
        /// in a timely fashion and avoid any time delay between local server close down and garbage collection by the .NET runtime.
        ///
        /// </remarks>
        public static void Dispose()
        {
            try
            {
                if (sharedSerial != null)
                {
                    sharedSerial.Dispose();
                }

            }
            catch
            {
            }

        }
        #endregion

        #region Single serial port connector

        // This region shows a way that a single serial port could be connected to by multiple drivers.
        // Connected is used to handle the connections to the port.
        // SendMessage is a way that messages could be sent to the hardware without conflicts between different drivers.
        //
        // All this is for a single connection, multiple connections would need multiple ports and a way to handle connecting and disconnection from them - see the multi driver handling section for ideas.

        /// <summary>
        /// Shared serial port
        /// </summary>
        public static Serial SharedSerial
        {
            get
            {
                return sharedSerial;
            }
        }

        /// <summary>
        /// The COM port name (e.g., "COM3") used for the serial connection to the mount.
        /// Set from the ASCOM Profile during driver initialization.
        /// </summary>
        public static string comPort { get; set; }

        /// <summary>
        /// Number of connections to the shared serial port
        /// </summary>
        public static int Connections
        {
            get
            {
                return serialConnectionCount;
            }

            set
            {
                serialConnectionCount = value;
            }
        }

        /// <summary>
        /// Sends a pre-framed LX200 command to the mount via the shared serial port and returns the response.
        /// </summary>
        /// <param name="command">The fully-framed command string to transmit (already includes protocol characters).</param>
        /// <param name="commandtype">
        /// Expected response type: 0 = blind (fire-and-forget, returns ""), 1 = boolean (single character,
        /// returns "True"/"False"), 2 = string (#-terminated response).
        /// </param>
        /// <returns>The mount's response as a string.</returns>
        /// <remarks>
        /// <para>Serialized via lock to prevent concurrent serial access from multiple driver instances.</para>
        /// <para>Clears serial buffers before each transmission to avoid stale data.</para>
        /// <para>Special case: when the <c>:MS#</c> (Move/Slew) command returns boolean true (object below
        /// horizon), the mount also sends a #-terminated error string that must be drained.</para>
        /// </remarks>
        public static string SendMessage(string command, int commandtype)
        {
            lock (lockObject)
            {

                SharedSerial.ClearBuffers();
                SharedSerial.Transmit(command);

                try
                {
                    switch (commandtype)
                    {
                        case 0:
                            TelescopeHardware.LogMessage("SendMessage", $"Blind - {command} Completed.");
                            Thread.Sleep(10); //Add a bit of waiting, equivalent to the waits from the receive methods
                            return "";

                        case 1:
                            stopwatch.Start();
                            var result = SharedSerial.ReceiveCounted(1);
                            stopwatch.Stop();
                            TelescopeHardware.LogMessage("SendMessage", $"Receive Bool execution time: {stopwatch.ElapsedMilliseconds}");
                            stopwatch.Reset();

                            bool retBool = char.GetNumericValue(result[0]) == 1; // Parse the returned string and create a boolean True / False value
                                                                                 //serialPort.ClearBuffers();
                            TelescopeHardware.LogMessage("SendMessage", $"Bool - {command} Completed: {result} Parsed as: {retBool}");
                            if (retBool && command.Equals(":MS#"))
                            {

                                var clrbuf = SharedSerial.ReceiveTerminated("#");
                                TelescopeHardware.LogMessage("SendMessage", $"Bool - Dumping String: {clrbuf}");

                            }
                            return retBool.ToString(); // Return the boolean value to the client

                        case 2:

                            stopwatch.Start();
                            string resp = SharedSerial.ReceiveTerminated("#");
                            stopwatch.Stop();
                            TelescopeHardware.LogMessage("SendMessage", $"Receive String execution time: {stopwatch.ElapsedMilliseconds}");
                            stopwatch.Reset();
                            TelescopeHardware.LogMessage("SendMessage", $"String - {command} Completed: {resp}");
                            return resp;
                    }
                    return "";
                }
                catch (Exception ex)
                {

                    TelescopeHardware.LogMessage("SendMessage", ex.Message);
                    throw ex;
                }

            }
        }

        /// <summary>
        /// Drains stale responses from the serial receive buffer after a command retry sequence.
        /// </summary>
        /// <remarks>
        /// <para>Called by <see cref="TelescopeHardware.Commander"/> after a successful retry to
        /// re-synchronize the command/response queue. The mount queues responses 1:1 with commands,
        /// so retransmitted commands create extra queued responses that must be cleared.</para>
        /// <para>Loops calling <see cref="Serial.Receive"/> until a timeout exception is thrown
        /// (indicating the buffer is empty). The timeout exception is intentionally swallowed.</para>
        /// </remarks>
        public static void ClearReTxBuff()
        {
            lock(lockObject)
            {
                try
                {
                    SharedSerial.ClearBuffers();
                    bool looper = true;
                    int iter = 0;
                    while(looper)
                    {
                        TelescopeHardware.LogMessage("ClearReTxBuff", $"Clearing Buffer.  Iteration: {iter}");
                        try
                        {
                            var buff = SharedSerial.Receive();
                        }
                        catch
                        {
                            looper = false;
                        }
                        
                        iter += 1;
                    }
                }
                catch (Exception ex)
                {
                    TelescopeHardware.LogMessage("ClearReTxBuff", $"Error: {ex.Message}");
                }
                    
            }
        }

        /// <summary>
        /// Gets or sets the shared serial port connection state with reference counting.
        /// </summary>
        /// <remarks>
        /// <para>Set <c>true</c>: if this is the first connection (count == 0), configures and opens
        /// the serial port (9600/8N1, 500ms timeout). Increments the connection count.</para>
        /// <para>Set <c>false</c>: decrements the connection count. When the count reaches zero,
        /// the serial port is closed.</para>
        /// <para>Get: returns the underlying serial port's connected state.</para>
        /// <para>All access is serialized via the shared lock object.</para>
        /// </remarks>
        public static bool Connected
        {
            set
            {
                lock (lockObject)
                {
                    if (value)
                    {
                        if (serialConnectionCount == 0)
                        {
                            SharedSerial.PortName = comPort;
                            SharedSerial.Speed = SerialSpeed.ps9600;
                            SharedSerial.Parity = SerialParity.None;
                            SharedSerial.DataBits = 8;
                            SharedSerial.StopBits = SerialStopBits.One;
                            SharedSerial.ReceiveTimeoutMs = RECEIVETIMEOUT;
                            SharedSerial.Connected = true;

                        }
                        serialConnectionCount++;
                        TelescopeHardware.LogMessage("SharedResources Connected Set", $"Connection count: {serialConnectionCount}");
                    }
                    else
                    {
                        serialConnectionCount--;
                        TelescopeHardware.LogMessage("SharedResources Connected Set", $"Disconnected.  Connections remaining: {serialConnectionCount}");
                        if (serialConnectionCount <= 0)
                        {
                            SharedSerial.Connected = false;            
                        }
                    }
                }
            }
            get { return SharedSerial.Connected; }
        }

        #endregion

    }

}
