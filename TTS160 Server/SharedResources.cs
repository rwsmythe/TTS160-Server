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
    /// Add and manage resources that are shared by all drivers served by this local server here.
    /// In this example it's a serial port with a shared SendMessage method an idea for locking the message and handling connecting is given.
    /// In reality extensive changes will probably be needed. 
    /// Multiple drivers means that several drivers connect to the same hardware device, aka a hub.
    /// Multiple devices means that there are more than one instance of the hardware, such as two focusers. In this case there needs to be multiple instances
    /// of the hardware connector, each with it's own connection count.
    /// </summary>
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
        /// Example of a shared SendMessage method
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        /// <remarks>
        /// The lock prevents different drivers tripping over one another. It needs error handling and assumes that the message will be sent unchanged and that the reply will always be terminated by a "#" character.
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
        /// Example of handling connecting to and disconnection from the shared serial port.
        /// </summary>
        /// <remarks>
        /// Needs error handling, the port name etc. needs to be set up first, this could be done by the driver checking Connected and if it's false setting up the port before setting connected to true.
        /// It could also be put here.
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
