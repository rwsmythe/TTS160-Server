//tabs=4
// --------------------------------------------------------------------------------
//
// ASCOM Telescope driver for TTS-160
//
// Description:	This driver is written for the TTS-160 Panther telescope.  In part, it uses code adapted
//              from the Meade LX200 Classic Driver (https://github.com/kickitharder/Meade-LX200-Classic--ASCOM-Driver)
//              as noted in inline comments within the code.  The implementation includes some
//              simulations and estimations required due to the limited implementation of the LX200 protocol
//              by the mount in an attempt to maximize the methods available to astro programs while
//              maintaining as close as possible to the ASCOM philosophy of reporting actual truth.
//
//              The driving force behind this driver was due to the issues surrounding the other available
//              drivers in use which were general LX200 implementations, particularly felt when ASCOM 6.6 was released
//              near the end of 2022.  This driver is intended to be able to be maintained by the Panther community
//              to prevent those issues from occuring in the future (or at least corrected more quickly!).
//
// Implements:	ASCOM Telescope interface version: 3
// Author:		Reid Smythe <rwsmythe@gmail.com>
//
// Edit Log:
//
// Date			Who	Vers	Description
// -----------	---	-----	-------------------------------------------------------
// 21JUN2024    RWS 354.1.2 Removed version from driver name in chooser.  Added slew result checking to indicate possible slew error if mount stops early.
// 20JUN2024    RWS 354.1.1 Corrected profile bug.  Refined Eq.Topo pulse guide.  Note that Conform will toss accuracy issues due to the mount reporting position only to the nearest second.
// 18JUN2024    RWS 354.1.0 Local server version released.
// 15JUN2024    RWS 354.0.0 Confirmed operation with 354 firmware.  Added Eq.Topo. Pulse Guide option.  Removed SlewToAltAz functionality.  Added ability for driver to remember site location
// 31AUG2023    RWS 353.0.0 Adding features for 353 firmware
// 21JUL2023    RWS 1.0.1   Added in selectable slew speeds
// 06JUL2023    RWS 1.0.1RC4 Added in guiding compensation in azimuth based off of target altitude
// 13JUN2023    RWS 1.0.1RC1 Troubleshooting missing pulseguide command and apparently stuck IsPulseGuiding value
// 09JUN2023    RWS 1.0.0   First release version
// 08JUN2023    RWS 0.9.5   Added in App Compatability feature for MPM and time sync feature
// 03JUN2023    RWS 0.9.4   Added in capability to add site elevation and adjust Slew Settling Time in setup dialog
// 29MAY2023    RWS 0.9.3   Corrected issues in Sync, UTCDate, SiderealTime, MoveAxis, and AxisRates
// 23MAY2023    RWS 0.9.1   Added in the native PulseGuide commands
// 21MAY2023    RWS 0.9.0   Passed ASCOM Compliance testing, ready to begin field testing
// 26APR2023    RWS 0.0.2   Further feature addition, commenced use of MiscResources
//                          in part to simulate features normally done in hardware
// 15APR2023	RWS	0.0.1	Initial edit, created from ASCOM driver template
// --------------------------------------------------------------------------------
//


// This is used to define code in the template that is specific to one class implementation
// unused code can be deleted and this definition removed.
#define Telescope

using ASCOM.Astrometry.AstroUtils;
using ASCOM.Astrometry.Transform;
using ASCOM.DeviceInterface;
using ASCOM.LocalServer;
using ASCOM.Utilities;
using System;
using System.Collections;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

namespace ASCOM.TTS160.Telescope
{
    //
    // Your driver's DeviceID is ASCOM.TTS160.Telescope
    //
    // The Guid attribute sets the CLSID for ASCOM.TTS160.Telescope
    // The ClassInterface/None attribute prevents an empty interface called
    // _TTS160 from being created and used as the [default] interface
    //
    //

    /// <summary>
    /// ASCOM Telescope Driver for TTS160.
    /// </summary>
    [HardwareClass()]
    internal static class TelescopeHardware
    {
        /// <summary>
        /// ASCOM DeviceID (COM ProgID) for this driver.
        /// The DeviceID is used by ASCOM applications to load the driver at runtime.
        /// </summary>
        internal static string driverID = "ASCOM.TTS160.Telescope";
        /// This driver is intended to specifically support TTS-160 Panther mount, based on the LX200 protocol.
        /// Driver description that displays in the ASCOM Chooser.
        /// </summary>
        private static readonly string driverVersion = "354.1.2";
        private static readonly string driverDescription = "TTS-160 v." + driverVersion;
        
        //private Serial serialPort; ->>>Moved to SharedSerial in SharedResources

        internal static string comPortProfileName = "COM Port"; // Constants used for Profile persistence
        internal static string comPortDefault = "COM1";
        internal static string traceStateProfileName = "Trace Level";
        internal static string traceStateDefault = "false";

        //internal static string comPort; // Variables to hold the current device configuration

        internal static string siteElevationProfileName = "Site Elevation";
        internal static string siteElevationDefault = "0";
        internal static string SlewSettleTimeName = "Slew Settle Time";
        internal static string SlewSettleTimeDefault = "2";
        internal static string SiteLatitudeName = "Site Latitude";
        internal static string SiteLatitudeDefault = "100";
        internal static string SiteLongitudeName = "Site Longitude";
        internal static string SiteLongitudeDefault = "200";
        internal static string CompatModeName = "Compatibility Mode";
        internal static string CompatModeDefault = "0";
        internal static string CanSetGuideRatesOverrideName = "CanSetGuideRates Override";
        internal static string CanSetGuideRatesOverrideDefault = "false";
        internal static string SyncTimeOnConnectName = "Sync Time on Connect";
        internal static string SyncTimeOnConnectDefault = "true";
        internal static string GuideCompName = "Guiding Compensation";
        internal static string GuideCompDefault = "0";
        internal static string GuideCompMaxDeltaName = "Guiding Compensation Max Delta";
        internal static string GuideCompMaxDeltaDefault = "1000";
        internal static string GuideCompBufferName = "Guiding Compensation Buffer";
        internal static string GuideCompBufferDefault = "20";
        internal static string TrackingRateOnConnectName = "Tracking Rate on Connect";
        internal static string TrackingRateOnConnectDefault = "0";
        internal static string PulseGuideEquFrameName = "PulseGuide Equatorial Frame";
        internal static string PulseGuideEquFrameDefault = "false";
        internal static string DriverSiteOverrideName = "Driver Site Override";
        internal static string DriverSiteOverrideDefault = "false";
        internal static string DriverSiteLatitudeName = "Driver Site Latitude";
        internal static string DriverSiteLatitudeDefault = "0";
        internal static string DriverSiteLongitudeName = "Driver Site Longitude";
        internal static string DriverSiteLongitudeDefault = "0";
        internal static string HCGuideRateName = "Handcontroller Guide Rate";
        internal static string HCGuideRateDefault = "2";

        internal static int MOVEAXIS_WAIT_TIME = 2000; //minimum delay between moveaxis commands

        private static string DriverProgId = ""; // ASCOM DeviceID (COM ProgID) for this driver, the value is set by the driver's class initialiser.
        private static string DriverDescription = ""; // The value is set by the driver's class initialiser.
        //internal static string comPort; // COM port name (if required)
        private static bool connectedState; // Local server's connected state
        private static bool runOnce = false; // Flag to enable "one-off" activities only to run once.
        internal static Util utilities; // ASCOM Utilities object for use as required
        internal static AstroUtils astroUtilities; // ASCOM AstroUtilities object for use as required
        internal static TraceLogger tl; // Local server's trace logger object for diagnostic log with information that you specify
        internal static readonly Transform T;  // Variable to provide coordinate Transforms
        internal static readonly object LockObject = new object();  // object used for locking to prevent multiple drivers accessing common code at the same time
        internal static ProfileProperties profileProperties = new ProfileProperties(); //Accessible profile to apply changes to

        /// <summary>
        /// Initializes a new instance of the <see cref="TTS160"/> class.
        /// Must be public for COM registration.
        /// </summary>
        static TelescopeHardware()
        {
            tl = new TraceLogger("", "TTS160.Hardware v. " + driverVersion);
            profileProperties = ReadProfile();
            tl.Enabled = profileProperties.TraceLogger;

            T = new Transform();

            LogMessage("Telescope", "Completed start-up");
        }

        internal static void InitializeHardware()
        {
            // This method will be called every time a new ASCOM client loads your driver
            LogMessage("InitializeHardware", $"Start.");

            // Make sure that "one off" activities are only undertaken once
            if (runOnce == false)
            {
                profileProperties = ReadProfile();
                LogMessage("InitializeHardware", $"Starting one-off initialization.");

                DriverDescription = Telescope.DriverDescription; // Get this device's Chooser description

                LogMessage("InitializeHardware", $"ProgID: {DriverProgId}, Description: {DriverDescription}");

                connectedState = false; // Initialise connected to false
                utilities = new Util(); //Initialise ASCOM Utilities object
                astroUtilities = new AstroUtils(); // Initialise ASCOM Astronomy Utilities object

                LogMessage("InitializeHardware", "Completed basic initialization");

                // Add your own "one off" device initialisation here e.g. validating existence of hardware and setting up communications

                Slewing = false;
                MiscResources.IsSlewing = false;
                MiscResources.IsSlewingToTarget = false;
                MiscResources.IsSlewingAsync = false;
                MiscResources.SlewSettleStart = DateTime.MinValue;
                MiscResources.IsPulseGuiding = false;
                MiscResources.MovingPrimary = false;
                MiscResources.MovingSecondary = false;
                MiscResources.EWPulseGuideFlag = false;
                MiscResources.NSPulseGuideFlag = false;

                LogMessage("InitializeHardware", $"One-off initialization complete.");
                runOnce = true; // Set the flag to ensure that this code is not run again
            }
        }


        //
        // PUBLIC COM INTERFACE ITelescopeV3 IMPLEMENTATION
        //

        #region Common properties and methods.

        /// <summary>
        /// Displays the Setup Dialog form.
        /// If the user clicks the OK button to dismiss the form, then
        /// the new settings are saved, otherwise the old values are reloaded.
        /// THIS IS THE ONLY PLACE WHERE SHOWING USER INTERFACE IS ALLOWED!
        /// </summary>
        public static void SetupDialog()
        {
            // consider only showing the setup dialog if not connected
            // or call a different dialog if connected
            if (IsConnected)
                System.Windows.Forms.MessageBox.Show("Already connected, just press OK");
            else 
            {
                profileProperties = ReadProfile();

                using (SetupDialogForm F = new SetupDialogForm(tl))
                {
                    F.SetProfile(profileProperties);
                    var result = F.ShowDialog();
                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        profileProperties = F.GetProfile(profileProperties);
                        WriteProfile(profileProperties); // Persist device configuration values to the ASCOM Profile store

                    }
                }
            }

        }

        /// <summary>Returns the list of custom action names supported by this driver.</summary>
        /// <value>An ArrayList of strings (SafeArray collection) containing the names of supported actions.</value>
        public static ArrayList SupportedActions
        {
            get
            {
                tl.LogMessage("SupportedActions Get", "Returning arraylist");
                return new ArrayList()
                {
                    "FieldRotationAngle"
                };
            }
        }

        /// <summary>Invokes the specified device-specific custom action.</summary>
        /// <param name="ActionName">A well known name agreed by interested parties that represents the action to be carried out.</param>
        /// <param name="ActionParameters">List of required parameters or an <see cref="String.Empty">Empty String</see> if none are required.</param>
        /// <returns>A string response. The meaning of returned strings is set by the driver author.
        /// <para>Suppose filter wheels start to appear with automatic wheel changers; new actions could be <c>QueryWheels</c> and <c>SelectWheel</c>. The former returning a formatted list
        /// of wheel names and the second taking a wheel name and making the change, returning appropriate values to indicate success or failure.</para>
        /// </returns>
        public static string Action(string actionName, string actionParameters)
        {
            tl.LogMessage("Action", "Action: " + actionName + "; Parameters: " + actionParameters);
            try
            {

                CheckConnected("Action");

                actionName = actionName.ToLower();
                switch (actionName)
                {

                    case "fieldrotationangle":
                        LogMessage("Action", "FieldRotationAngle - Retrieving FieldRotationAngle");
                        var result = Commander(":ra#", true, 2);
                        LogMessage("Action", "FieldRotationAngle - Retrieved String: " + result);
                        return result;

                    default:
                        throw new ASCOM.ActionNotImplementedException("Action " + actionName + " is not implemented by this driver");
                }

            }
            catch (Exception ex)
            {
                LogMessage("Action", $"Error: {ex.Message}");
                throw;
            }

        }

        internal static string Commander(string command, bool raw, int commandtype)
        {
            lock (LockObject)
            {
                CheckConnected("Commander");
                CheckParked("Commander");

                if (!raw) { command = ":" + command + "#"; }
                try
                {
                    switch (commandtype)
                    {
                        case 0:
                            try
                            {
                                LogMessage("Commander", $"Blind - raw: {raw} command {command}");
                                SharedResources.SendMessage(command, commandtype);
                                LogMessage("Commander", $"Blind - {command} Completed");
                                return "";
                            }
                            catch (Exception ex)
                            {
                                LogMessage("Commander", $"Blind - Error: {ex.Message}; Command: {command}");
                                throw;
                            }
                        case 1:
                            try
                            {
                                LogMessage("Commander", $"Bool - raw: {raw} command {command}");
                                string retbool = SharedResources.SendMessage(command, commandtype);
                                return retbool;
                            }
                            catch (Exception ex)
                            {
                                LogMessage("Commander", $"Bool - Error: {ex.Message}; Command: {command}");
                                throw;
                            }
                        case 2:
                            try
                            {
                                LogMessage("Commander", $"String - raw: {raw} command {command}");
                                var result = SharedResources.SendMessage(command, commandtype);  //assumes that all return strings are # terminated...is this true?
                                                                                                        //tl.LogMessage("CommandString", "utilities.WaitForMilliseconds(TRANSMIT_WAIT_TIME);");
                                                                                                        //utilities.WaitForMilliseconds(TRANSMIT_WAIT_TIME); //limit transmit rate
                                                                                                        //tl.LogMessage("CommandString", "completed serial port receive...");
                                LogMessage("Commander", $"String - {command} Completed: {result}");
                                return result;
                            }
                            catch (Exception ex)
                            {
                                LogMessage("Commander", $"String - Error: {ex.Message}; Command: {command}");
                                throw;
                            }
                        default:
                            throw new ASCOM.DriverException("Invalid Command Type: " + commandtype.ToString());
                    }
                }
                catch (Exception ex)
                {
                    LogMessage("Commander", $"Error: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Transmits an arbitrary string to the device and does not wait for a response.
        /// Optionally, protocol framing characters may be added to the string before transmission.
        /// </summary>
        /// <param name="Command">The literal command string to be transmitted.</param>
        /// <param name="Raw">
        /// if set to <c>true</c> the string is transmitted 'as-is'.
        /// If set to <c>false</c> then protocol framing characters may be added prior to transmission.
        /// </param>
        public static void CommandBlind(string command, bool raw)
        {

            LogMessage("CommandBlind", "Not implemented");
            throw new MethodNotImplementedException("CommandBlind");

        }

        /// <summary>
        /// Transmits an arbitrary string to the device and waits for a boolean response.
        /// Optionally, protocol framing characters may be added to the string before transmission.
        /// </summary>
        /// <param name="Command">The literal command string to be transmitted.</param>
        /// <param name="Raw">
        /// if set to <c>true</c> the string is transmitted 'as-is'.
        /// If set to <c>false</c> then protocol framing characters may be added prior to transmission.
        /// </param>
        /// <returns>
        /// Returns the interpreted boolean response received from the device.
        /// </returns>
        public static bool CommandBool(string command, bool raw)
        {

            LogMessage("CommandBool", "Not implemented");
            throw new MethodNotImplementedException("CommandBool");

        }

        /// <summary>
        /// Transmits an arbitrary string to the device and waits for a string response.
        /// Optionally, protocol framing characters may be added to the string before transmission.
        /// </summary>
        /// <param name="Command">The literal command string to be transmitted.</param>
        /// <param name="Raw">
        /// if set to <c>true</c> the string is transmitted 'as-is'.
        /// If set to <c>false</c> then protocol framing characters may be added prior to transmission.
        /// </param>
        /// <returns>
        /// Returns the string response received from the device.
        /// </returns>
        public static string CommandString(string command, bool raw)
        {

            LogMessage("CommandString", "Not implemented");
            throw new MethodNotImplementedException("CommandBool");

        }

        /// <summary>
        /// Dispose the late-bound interface, if needed. Will release it via COM
        /// if it is a COM object, else if native .NET will just dereference it
        /// for GC.
        /// </summary>
        public static void Dispose()
        {
            // Clean up the trace logger and util objects
            tl.Enabled = false;
            tl.Dispose();
            tl = null;
            utilities.Dispose();
            utilities = null;
            astroUtilities.Dispose();
            astroUtilities = null;
            T.Dispose();
     
        }

        /// <summary>
        /// Set True to connect to the device hardware. Set False to disconnect from the device hardware.
        /// You can also read the property to check whether it is connected. This reports the current hardware state.
        /// </summary>
        /// <value><c>true</c> if connected to the hardware; otherwise, <c>false</c>.</value>
        public static bool Connected
        {
            get
            {
                LogMessage("Connected get", $"{IsConnected}");
                return IsConnected;
            }
            set
            {
                //MessageBox.Show("Wait!");
                LogMessage("Connected set", $"{value}");

                if (value)
                {
                    if (!SharedResources.Connected)  //Check to see if we are connected at all
                    {
                        LogMessage("Connected Set", "No existing connection found, performing first time connect...");
                        //First time connection
                        try
                        {
                            if (AtPark)
                            {
                                LogMessage("Connected set", "Mount appears parked.  Cycle power and disconnect from all programs to connect");
                                throw new ASCOM.ParkedException("Mount appears parked.  Cycle mount power and disconnect from all programs to connect");
                            }

                            //Define new serial object.  TTS-160 connects at 9600 baud, 8 data, no parity, 1 stop
                            SharedResources.comPort = profileProperties.ComPort;
                            LogMessage("Connected set", $"Connecting to {profileProperties.ComPort}");
                            SharedResources.Connected = true;
                            connectedState = true;
                        }
                        catch (Exception ex)
                        {
                            LogMessage("Connected set", $"Error when connecting: {ex.Message}");
                            connectedState = false;
                            throw;
                        }

                        try
                        {
                            LogMessage("Connected set", "Success");
                            LogMessage("Connected set", "Connected with " + driverDescription);
                            LogMessage("Connected set", "Updating Site Lat and Long");
                            profileProperties.SiteLatitude = SiteLatitudeInit;
                            profileProperties.SiteLongitude = SiteLongitudeInit;
                            LogMessage("Connected set", $"Mount Lat: {profileProperties.SiteLatitude}");
                            LogMessage("Connected set", $"Mount Long: {profileProperties.SiteLongitude}");
                            LogMessage("Connected set", $"Driver Site Location Override: {profileProperties.DriverSiteOverride}");
                            LogMessage("Connected set", $"Driver Lat: {profileProperties.DriverSiteLatitude}");
                            LogMessage("Connected set", $"Driver Long: {profileProperties.DriverSiteLongitude}");
                            LogMessage("Connected set", $"Equatorial Pulse Guide: {profileProperties.PulseGuideEquFrame}");
                            WriteProfile(profileProperties);

                            if (profileProperties.SyncTimeOnConnect)
                            {
                                LogMessage("Connected set", "Sync Time on Connect - " + profileProperties.SyncTimeOnConnect.ToString());
                                LogMessage("Connected set", "Pre Sync Mount UTC: " + UTCDate.ToString("MM/dd/yy HH:mm:ss"));
                                LogMessage("Connected set", "Pre Sync Computer UTC: " + DateTime.UtcNow.ToString("MM/dd/yy HH:mm:ss"));                              
                                UTCDate = DateTime.UtcNow;
                                LogMessage("Connected set", "Post Sync Mount UTC: " + UTCDate.ToString("MM/dd/yy HH:mm:ss"));
                                LogMessage("Connected set", "Post Sync Computer UTC: " + DateTime.UtcNow.ToString("MM/dd/yy HH:mm:ss"));
                            }

                            LogMessage("Connected", "Establishing Tracking Rate - " + profileProperties.TrackingRateOnConnect.ToString());
                            switch (profileProperties.TrackingRateOnConnect)
                            {

                                case 0:
                                    TrackingRate = DriveRates.driveSidereal;
                                    break;
                                case 1:
                                    TrackingRate = DriveRates.driveLunar;
                                    break;
                                case 2:
                                    TrackingRate = DriveRates.driveSolar;
                                    break;
                                default:
                                    throw new ASCOM.InvalidValueException("Unexpected TrackingRateOnConnect Value: " + profileProperties.TrackingRateOnConnect.ToString());

                            }

                            MiscResources.IsTargetDecSet = false; //'Clearing' any previous target info
                            MiscResources.IsTargetRASet = false;
                            MiscResources.IsTargetSet = false;
                        }
                        catch (Exception ex)
                        {
                            LogMessage("Connected set", $"Error during initial configuration: {ex.Message}");                           
                        }                    
                    }
                    else
                    {
                        LogMessage("Connected set", "Existing connection found, incrementing count...");
                        SharedResources.Connected = true;
                    }
                }
                else
                {
                    LogMessage("Connected set", $"Disconnecting from port {profileProperties.ComPort}");

                    try
                    {
                        profileProperties.SiteLatitude = SiteLatitudeInit;
                        profileProperties.SiteLongitude = SiteLongitudeInit;
                        WriteProfile(profileProperties);

                        SharedResources.Connected = false;
                        if (!SharedResources.Connected) //Check to see if any 
                        {
                            connectedState = false;
                            LogMessage("Connected set", "All drivers disconnected, disconnecting from hardware.");
                        }
    
                    }
                    catch (Exception ex)
                    {

                        throw new ASCOM.DriverException($"Serial port disconnect error: {ex}");

                    }

                }
            }
        }

        /// <summary>
        /// Returns a description of the device, such as manufacturer and modelnumber. Any ASCII characters may be used.
        /// </summary>
        /// <value>The description.</value>
        public static string Description
        {
            // TODO customise this device description
            get
            {
                LogMessage("Description get", driverDescription);
                return driverDescription;
            }
        }

        /// <summary>
        /// Descriptive and version information about this ASCOM driver.
        /// </summary>
        public static string DriverInfo
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                // TODO customise this driver description
                string driverInfo = "Driver for TTS-160. Version: " + String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
                tl.LogMessage("DriverInfo get", driverInfo);
                return driverInfo;
            }
        }

        /// <summary>
        /// A string containing only the major and minor version of the driver formatted as 'm.n'.
        /// </summary>
        public static string DriverVersion
        {
            get
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string driverVersion = String.Format(CultureInfo.InvariantCulture, $"{version.Major}.{version.Minor}");
                LogMessage("DriverVersion get", driverVersion);
                return driverVersion;
            }
        }

        /// <summary>
        /// The interface version number that this device supports. 
        /// </summary>
        public static short InterfaceVersion
        {
            //Interface version 3...soon to be 4?
            get
            {
                LogMessage("InterfaceVersion Get", "3");
                return Convert.ToInt16("3");
            }
        }

        /// <summary>
        /// The short name of the driver, for display purposes
        /// </summary>
        public static string Name
        {
            get
            {
                string name = "TTS-160";
                LogMessage("Name Get", name);
                return name;
            }
        }

        #endregion

        #region ITelescope Implementation

        /// <summary>
        /// Stops a slew in progress.
        /// </summary>
        internal static void AbortSlew()
        {
            try
            {
                //Per ASCOM standards, should not send command unless "Slewing" is true.
                //TTS-160 will ignore this command if it is not slewing, so this allows 'universal abort'
                //This provides a measure of safety in case something goes wrong with the Slewing property
                //or IsSlewingToTarget variable in MiscResources.

                tl.LogMessage("AbortSlew", "Aborting Slew, CommandBlind :Q#");
                CheckConnected("AbortSlew");
                Commander(":Q#", true, 0);
                Slewing = false;
                MiscResources.IsSlewingAsync = false;
                MiscResources.IsSlewingToTarget = false;
                MiscResources.SlewSettleStart = DateTime.MinValue;
                IsPulseGuiding = false;
                MiscResources.IsPulseGuiding = false;
                MiscResources.MovingPrimary = false;
                MiscResources.MovingSecondary = false;
                Tracking = MiscResources.TrackSetFollower;

                LogMessage("AbortSlew", "Completed");
            }
            catch (Exception ex)
            {
                LogMessage("AbortSlew", $"Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// The alignment mode of the mount (Alt/Az, Polar, German Polar).
        /// </summary>
        internal static AlignmentModes AlignmentMode
        {
            get
            {
                try
                {
                    CheckConnected("AlignmentMode");
                    String ret = Commander(":GW#", true, 2);
                    switch (ret[0])
                    {
                        case 'A': return DeviceInterface.AlignmentModes.algAltAz;
                        case 'P': return DeviceInterface.AlignmentModes.algPolar;  //This should be the only response from TTS-160
                        case 'G': return DeviceInterface.AlignmentModes.algGermanPolar;
                        default: throw new DriverException("Unknown AlignmentMode Reported");
                    }

                }
                catch (Exception ex)
                {
                    LogMessage("AlignmentMode Get", $"Error: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// The Altitude above the local horizon of the telescope's current position (degrees, positive up)
        /// </summary>
        internal static double Altitude
        {
            get
            {
                try
                {
                    CheckConnected("Altitude Get");
                    LogMessage("Altitude get", "Getting Altitude");
                    var result = Commander(":GA#", true, 2);
                    //:GA# Get telescope altitude
                    //Returns: DDD*MM# or DDD*MM'SS#
                    //The current telescope Altitude depending on the selected precision.

                    double alt = utilities.DMSToDegrees(result);
                    LogMessage("Altitude Get", $"{alt}");
                    return alt;
                }
                catch (Exception ex)
                {
                    LogMessage("Altitude Get", $"Error: {ex.Message}");
                    throw;
                }

            }
        }

        /// <summary>
        /// The area of the telescope's aperture, taking into account any obstructions (square meters)
        /// </summary>
        internal static double ApertureArea
        {
            get
            {
                LogMessage("ApertureArea Get", "Not implemented");
                throw new PropertyNotImplementedException("ApertureArea", false);
            }
        }

        /// <summary>
        /// The telescope's effective aperture diameter (meters)
        /// </summary>
        internal static double ApertureDiameter
        {
            get
            {
                LogMessage("ApertureDiameter Get", "Not implemented");
                throw new PropertyNotImplementedException("ApertureDiameter", false);
            }
        }

        /// <summary>
        /// True if the telescope is stopped in the Home position. Set only following a <see cref="FindHome"></see> operation,
        /// and reset with any slew operation. This property must be False if the telescope does not support homing.
        /// </summary>
        internal static bool AtHome
        {
            get
            {
                //TTS-160 does not support Homing at this time.  Return False.
                LogMessage("AtHome get", $"{false}");
                return false;
            }
        }

        /// <summary>
        /// True if the telescope has been put into the parked state by the <see cref="Park" /> method. Set False by calling the Unpark() method.
        /// </summary>
        internal static bool AtPark
        {
            get
            {
                tl.LogMessage("AtPark get", $"{MiscResources.IsParked}");
                return MiscResources.IsParked;
            }
            set
            {
                tl.LogMessage("AtPark set", $"{value}");
                MiscResources.IsParked = value;
            }
        }

        /// <summary>
        /// Determine the rates at which the telescope may be moved about the specified axis by the <see cref="MoveAxis" /> method.
        /// </summary>
        /// <param name="Axis">The axis about which rate information is desired (TelescopeAxes value)</param>
        /// <returns>Collection of <see cref="IRate" /> rate objects</returns>
        internal static IAxisRates AxisRates(TelescopeAxes Axis)
        {
            try
            {
                CheckConnected("AxisRates");
                LogMessage("AxisRates get", $"{Axis}");
                var buf = new AxisRates(Axis);
                LogMessage("AxisRates get", $"Returning - {buf}; Count: {buf.Count}");
                return buf;
            }
            catch (Exception ex)
            {
                LogMessage("AxisRates get", $"Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// The azimuth at the local horizon of the telescope's current position (degrees, North-referenced, positive East/clockwise).
        /// </summary>
        internal static double Azimuth
        {
            get
            {
                try
                {

                    LogMessage("Azimuth get", "Getting Azimuth");
                    CheckConnected("Azimuth get");

                    var result = Commander(":GZ#", true, 2);
                    //:GZ# Get telescope azimuth
                    //Returns: DDD*MM#T or DDD*MM'SS# verify low precision returns with T at the end!
                    //The current telescope Azimuth depending on the selected precision.

                    double az = utilities.DMSToDegrees(result);
                    LogMessage("Azimuth get", $"{az}");
                    return az;

                }
                catch (Exception ex)
                {
                    LogMessage("Azimuth get", $"Error: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// True if this telescope is capable of programmed finding its home position (<see cref="FindHome" /> method).
        /// </summary>
        internal static bool CanFindHome
        {
            get
            {
                try
                {
                    CheckConnected("CanFindHome");

                    //TTS-160 does not have 'Home' functionality implemented at this time, return false
                    LogMessage("CanFindHome get", $"{false}");
                    return false;
                }
                catch (Exception ex)
                {
                    LogMessage("CanFindHome", $"Error: {ex.Message}");
                    throw;
                }

            }
        }

        /// <summary>
        /// True if this telescope can move the requested axis
        /// </summary>
        internal static bool CanMoveAxis(TelescopeAxes Axis)
        {
            try
            {
                CheckConnected("CanMoveAxis");
                LogMessage("CanMoveAxis get", $"{Axis}");
                switch (Axis)
                {
                    case TelescopeAxes.axisPrimary: return true;
                    case TelescopeAxes.axisSecondary: return true;
                    case TelescopeAxes.axisTertiary: return false;
                    default: throw new InvalidValueException("CanMoveAxis", Axis.ToString(), "0 to 2");
                }
            }
            catch (Exception ex)
            {
                LogMessage("CanFindHome", $"Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// True if this telescope is capable of programmed parking (<see cref="Park" />method)
        /// </summary>
        internal static bool CanPark
        {
            get
            {
                try
                {
                    CheckConnected("CanPark");

                    LogMessage("CanPark get", $"{true}");
                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage("CanPark", $"Error: {ex.Message}");
                    throw;
                }

            }
        }

        /// <summary>
        /// True if this telescope is capable of software-pulsed guiding (via the <see cref="PulseGuide" /> method)
        /// </summary>
        internal static bool CanPulseGuide
        {
            get
            {
                try
                {
                    CheckConnected("CanPulseGuide");

                    //Pulse guiding is implemented => true
                    LogMessage("CanPulseGuide get", $"{true}");
                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage("CanPulseGuide", $"Error: {ex.Message}");
                    throw;
                }


            }
        }

        /// <summary>
        /// True if the <see cref="DeclinationRate" /> property can be changed to provide offset tracking in the declination axis.
        /// </summary>
        internal static bool CanSetDeclinationRate
        {
            get
            {
                try
                {
                    CheckConnected("CanSetDeclinationRate");

                    //SetDeclinationRate is not implemented in TTS-160, return false
                    LogMessage("CanSetDeclinationRate", $"{false}");
                    return false;
                }
                catch (Exception ex)
                {
                    LogMessage("CanSetDeclinationRate", $"Error: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// True if the guide rate properties used for <see cref="PulseGuide" /> can ba adjusted.
        /// </summary>
        internal static bool CanSetGuideRates
        {
            get
            {
                try
                {
                    CheckConnected("CanSetGuideRates");

                    //Check for override for App Compatibility Mode
                    if (profileProperties.CanSetGuideRatesOverride)
                    {
                        LogMessage("CanSetGuideRates Override", "Showing CanSetGuideRates as True");
                        LogMessage("CanSetGuideRates get", $"{true}");
                        return true;
                    }
                    else
                    {
                        //TTS-160 does not support SetGuideRates, return false
                        LogMessage("CanSetGuideRates", $"{false}");
                        return false;
                    }

                }
                catch (Exception ex)
                {
                    LogMessage("CanSetGuideRates", $"Error: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// True if this telescope is capable of programmed setting of its park position (<see cref="SetPark" /> method)
        /// </summary>
        internal static bool CanSetPark
        {
            get
            {
                try
                {
                    CheckConnected("CanSetPark");

                    //Set Park is not implemented by TTS-160, return false
                    LogMessage("CanSetPark", "Get - " + false.ToString());
                    return false;
                }
                catch (Exception ex)
                {
                    LogMessage("CanSetPark", $"Error: {ex.Message}");
                    throw;
                }

            }
        }

        /// <summary>
        /// True if the <see cref="SideOfPier" /> property can be set, meaning that the mount can be forced to flip.
        /// </summary>
        internal static bool CanSetPierSide
        {
            get
            {
                try
                {
                    CheckConnected("CanSetPierSide");

                    //TTS-160 does not have Set PierSide implemented, return false
                    LogMessage("CanSetPierSide get", $"{false}");
                    return false;
                }
                catch (Exception ex)
                {
                    LogMessage("CanSetPierSide", $"Error: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// True if the <see cref="RightAscensionRate" /> property can be changed to provide offset tracking in the right ascension axis.
        /// </summary>
        internal static bool CanSetRightAscensionRate
        {
            get
            {
                try
                {
                    CheckConnected("CanSetRightAscensionRate");

                    //TTS-160 has not implemented SetRightAscensionRate, return false
                    LogMessage("CanSetRightAscensionRate get", $"{false}");
                    return false;
                }
                catch (Exception ex)
                {
                    LogMessage("CanSetRightAscensionRate", $"Error: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// True if the <see cref="Tracking" /> property can be changed, turning telescope sidereal tracking on and off.
        /// </summary>
        internal static bool CanSetTracking
        {
            get
            {
                try
                {

                    CheckConnected("CanSetTracking");

                    LogMessage("CanSetTracking", "Get - " + true.ToString());
                    return true;

                }
                catch (Exception ex)
                {
                    LogMessage("CanSetTracking", $"Error: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// True if this telescope is capable of programmed slewing (synchronous or asynchronous) to equatorial coordinates
        /// </summary>
        internal static bool CanSlew
        {
            get
            {
                try
                {
                    CheckConnected("CanSlew");

                    LogMessage("CanSlew", "Get - " + true.ToString());
                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage("CanSlew", $"Error: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// True if this telescope is capable of programmed slewing (synchronous or asynchronous) to local horizontal coordinates
        /// </summary>
        internal static bool CanSlewAltAz
        {
            get
            {

                try
                {
                    CheckConnected("CanSlewAltAz");

                    LogMessage("CanSlewAltAz", "Get - " + false.ToString());
                    return false;
                }
                catch (Exception ex)
                {
                    LogMessage("CanSlewAltAz", $"Error: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// True if this telescope is capable of programmed asynchronous slewing to local horizontal coordinates
        /// </summary>
        internal static bool CanSlewAltAzAsync
        {

            get
            {
                try
                {
                    CheckConnected("CanSlewAltAzAsync");

                    LogMessage("CanSlewAltAzAsync", $"{false}");
                    return false;
                }
                catch (Exception ex)
                {
                    LogMessage("CanSlewAltAzAsync", $"Error: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// True if this telescope is capable of programmed asynchronous slewing to equatorial coordinates.
        /// </summary>
        internal static bool CanSlewAsync
        {
            get
            {
                try
                {
                    CheckConnected("CanSlewAsync");

                    LogMessage("CanSlewAsync get", $"{true}");
                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage("CanSlewAsync", $"Error: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// True if this telescope is capable of programmed synching to equatorial coordinates.
        /// </summary>
        internal static bool CanSync
        {
            get
            {
                try
                {
                    CheckConnected("CanSync");

                    LogMessage("CanSync get", $"{true}");
                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage("CanSync", $"Error: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// True if this telescope is capable of programmed synching to local horizontal coordinates
        /// </summary>
        internal static bool CanSyncAltAz
        {
            get
            {
                try
                {
                    CheckConnected("CanSyncAltAz");

                    LogMessage("CanSyncAltAz get", $"{true}");
                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage("CanSyncAltAz", $"Error: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// True if this telescope is capable of programmed unparking (<see cref="Unpark" /> method).
        /// </summary>
        internal static bool CanUnpark
        {
            get
            {
                try
                {
                    CheckConnected("CanUnpark");

                    LogMessage("CanUnpark get", $"{false}");
                    return false;
                }
                catch (Exception ex)
                {
                    LogMessage("CanUnpark", $"Error: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// The declination (degrees) of the telescope's current equatorial coordinates, in the coordinate system given by the <see cref="EquatorialSystem" /> property.
        /// Reading the property will raise an error if the value is unavailable.
        /// </summary>
        internal static double Declination
        {
            get
            {
                try
                {

                    //tl.LogMessage("Declination Get", "Getting Declination");
                    CheckConnected("Declination Get");

                    //var result = CommandString(":GD#", true);
                    var result = Commander(":GD#", true, 2);
                    //:GD# Get telescope Declination
                    //Returns: DDD*MM#T or DDD*MM'SS#
                    //The current telescope Declination depending on the selected precision.

                    double declination = utilities.DMSToDegrees(result);

                    LogMessage("Declination get", utilities.DegreesToDMS(declination, ":", ":",""));
                    return declination;

                }
                catch (Exception ex)
                {
                    LogMessage("Declination Get", $"Error: {ex.Message}");
                    throw;
                }

            }
        }

        /// <summary>
        /// The declination tracking rate (arcseconds per SI second, default = 0.0)
        /// </summary>
        internal static double DeclinationRate
        {
            get
            {
                //Declination Rate not implemented by TTS-160, return 0.0
                double declinationRate = 0.0;
                LogMessage("DeclinationRate get", $"{declinationRate}");
                return declinationRate;
            }
            set
            {
                //Declination Rate not implemented by TTS-160
                LogMessage("DeclinationRate Set", "Not implemented");
                throw new PropertyNotImplementedException("DeclinationRate", true);
            }
        }

        /// <summary>
        /// Predict side of pier for German equatorial mounts at the provided coordinates
        /// </summary>
        internal static PierSide DestinationSideOfPier(double rightAscension, double Declination)
        {
            
            try
            {
                CheckConnected("DestinationSideOfPier");

                var destinationSOP = CalculateSideOfPier(rightAscension);

                LogMessage("DestinationSideOfPier",
                    $"Destination SOP of RA {rightAscension.ToString(CultureInfo.InvariantCulture)} is {destinationSOP}");

                return destinationSOP;
            }
            catch (Exception ex)
            {
                LogMessage("DestinationSideOfPier", $"Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// True if the telescope or driver applies atmospheric refraction to coordinates.
        /// </summary>
        internal static bool DoesRefraction
        {
            get
            {
                //Refraction not implemented by TTS-160
                LogMessage("DoesRefraction get", "Not implemented");
                throw new PropertyNotImplementedException("DoesRefraction", false);
            }
            set
            {
                //Refraction not implemented by TTS-160
                LogMessage("DoesRefraction set", "Not implemented");
                throw new PropertyNotImplementedException("DoesRefraction", true);
            }
        }

        /// <summary>
        /// Equatorial coordinate system used by this telescope (e.g. Topocentric or J2000).
        /// </summary>
        internal static EquatorialCoordinateType EquatorialSystem
        {
            get
            {
                try
                {
                    CheckConnected("EquatorialCoordinateType");

                    //TTS-160 uses accepts Topocentric coordinates

                    EquatorialCoordinateType equatorialSystem = EquatorialCoordinateType.equTopocentric;
                    LogMessage("EquatorialCoordinateType get", $"{equatorialSystem}");
                    return equatorialSystem;
                }
                catch (Exception ex)
                {
                    LogMessage("EquatorialCoordinateType", $"Error: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Locates the telescope's "home" position (synchronous)
        /// </summary>
        internal static void FindHome()
        {
            LogMessage("FindHome", "Not implemented");
            throw new MethodNotImplementedException("FindHome");
        }

        /// <summary>
        /// The telescope's focal length, meters
        /// </summary>
        internal static double FocalLength
        {
            get
            {
                LogMessage("FocalLength Get", "Not implemented");
                throw new PropertyNotImplementedException("FocalLength", false);
            }
        }

        /// <summary>
        /// The current Declination movement rate offset for telescope guiding (degrees/sec)
        /// </summary>
        internal static double GuideRateDeclination
        {
            get
            {

                double guiderate = 0;

                switch (profileProperties.HCGuideRate)
                {
                    case 0:
                        guiderate = 1.0 / 3600.0;
                        break;
                    case 1:
                        guiderate = 3.0 / 3600.0;
                        break;
                    case 2:
                        guiderate = 5.0 / 3600.0;
                        break;
                    case 3:
                        guiderate = 10.0 / 3600.0;
                        break;
                    case 4:
                        guiderate = 20.0 / 3600.0;
                        break;
                }

                LogMessage("GuideRateDeclination get", $"HCGuideRate: {profileProperties.HCGuideRate} = {guiderate} deg/sec");

                return guiderate;

            }
            set
            {
                //Check for CanSetGuideRates Override...
                if (profileProperties.CanSetGuideRatesOverride)
                {
                    LogMessage("CanSetGuideRates Override", "Set GuideRateDeclination " + value.ToString() + " command received");
                    LogMessage("GuideRateDeclination set", $"{value} assigned to nothing...");
                }
                else
                {
                    LogMessage("GuideRateDeclination Set", "Not implemented");
                    throw new PropertyNotImplementedException("GuideRateDeclination", true);
                }

            }
        }

        /// <summary>
        /// The current Right Ascension movement rate offset for telescope guiding (degrees/sec)
        /// </summary>
        internal static double GuideRateRightAscension
        {
            get
            {

                //ASCOM standards as this returned in deg/sec, NOT sidereal hours/sec!

                double guiderate = 0;

                switch (profileProperties.HCGuideRate)
                {
                    case 0:
                        guiderate = 1.0 / 3600.0;
                        break;
                    case 1:
                        guiderate = 3.0 / 3600.0;
                        break;
                    case 2:
                        guiderate = 5.0 / 3600.0;
                        break;
                    case 3:
                        guiderate = 10.0 / 3600.0;
                        break;
                    case 4:
                        guiderate = 20.0 / 3600.0;
                        break;
                }

                LogMessage("GuideRateRightAscension get", $"HCGuideRate: {profileProperties.HCGuideRate} = {guiderate} deg/sec");

                return guiderate;

            }
            set
            {
                //Check for CanSetGuideRates Override...
                if (profileProperties.CanSetGuideRatesOverride)
                {
                    LogMessage("CanSetGuideRates Override set", $"GuideRateRightAscension {value} command received");
                    LogMessage("GuideRateRightAscension set", $"{value} assigned to nothing...");
                }
                else
                {
                    LogMessage("GuideRateRightAscension Set", "Not implemented");
                    throw new PropertyNotImplementedException("GuideRateRightAscension", true);
                }
            }
        }

        /// <summary>
        /// True if a <see cref="PulseGuide" /> command is in progress, False otherwise
        /// </summary>
        internal static bool IsPulseGuiding
        {
            //Pulse Guide query is not implemented in TTS-160 => track in driver
            get
            {
                try
                {
                    CheckConnected("IsPulseGuiding");

                    if (MiscResources.EWPulseGuideFlag)
                    {
                        if (DateTime.Now > MiscResources.EWPulseGuideFinish)
                        {
                            MiscResources.EWPulseGuideFlag = false;
                        }
                        else
                        {
                            LogMessage("IsPulseGuiding get", "Still Guiding EW");
                        }
                    }

                    if (MiscResources.NSPulseGuideFlag)
                    {
                        if (DateTime.Now > MiscResources.NSPulseGuideFinish)
                        {
                            MiscResources.NSPulseGuideFlag = false;
                        }
                        else
                        {
                            LogMessage("IsPulseGuiding get", "Still Guiding NS");
                        }


                    }

                    if (!MiscResources.EWPulseGuideFlag && !MiscResources.NSPulseGuideFlag)
                    {
                        MiscResources.IsPulseGuiding = false;
                    }

                    LogMessage("IsPulseGuiding get", $"{MiscResources.IsPulseGuiding}");
                    return MiscResources.IsPulseGuiding;
                }
                catch (Exception ex)
                {
                    LogMessage("IsPulseGuiding", $"Error: {ex.Message}");
                    throw;
                }
            }

            set
            {
                LogMessage("IsPulseGuding set", $"{value}");
                MiscResources.IsPulseGuiding = value;
            }
        }

        /// <summary>
        /// Move the telescope in one axis at the given rate.
        /// </summary>
        /// <param name="Axis">The physical axis about which movement is desired</param>
        /// <param name="Rate">The rate of motion (deg/sec) about the specified axis</param>
        // Implementation of this function based on code from Generic Meade Driver
        internal static void MoveAxis(TelescopeAxes Axis, double Rate)
        {
            try
            {

                LogMessage("MoveAxis", $"Axis={Axis} rate={Rate}");
                CheckConnected("MoveAxis");
                CheckParked("MoveAxis");

                if (!MiscResources.MovingPrimary && !MiscResources.MovingSecondary && Slewing)
                {
                    throw new ASCOM.InvalidOperationException("Error: Non-MoveAxis motion detected, MoveAxis unavailable");
                }

                var absRate = Math.Abs(Rate);
                LogMessage("MoveAxis", $"Setting rate to {absRate} deg/sec");

                switch (absRate)
                {

                    case (0):
                        //do nothing, it's ok this time as we're halting the slew.
                        break;

                    case (0.000277777777777778):
                        Commander(":RG#", true, 0);
                        break;

                    case (1.4):
                        Commander(":RM#", true, 0);
                        break;

                    case (2.2):
                        Commander(":RC#", true, 0);
                        break;

                    case (3):
                        Commander(":RS#", true, 0);
                        break;

                    default:
                        //invalid rate exception
                        throw new InvalidValueException($"Rate {absRate} deg/sec not supported");
                }

                int LOOP_WAIT_TIME = 100; //ms
                int iter = 0;
                int i = 0;
                switch (Axis)
                {
                    case TelescopeAxes.axisPrimary:
                        switch (Rate.Compare(0))
                        {
                            case ComparisonResult.Equals:
                                tl.LogMessage("MoveAxis", "Primary Axis Stop Movement");
                                Commander(":Qe#", true, 0);
                                //:Qe# Halt eastward Slews
                                //Returns: Nothing
                                Commander(":Qw#", true, 0);

                                //:Qw# Halt westward Slews
                                //Returns: Nothing                              

                                //Redo this implementation for async operation.  Add an initial check to verify moving axis state and whether it should be timed out or not

                                iter = Convert.ToInt32(Convert.ToDouble(MOVEAXIS_WAIT_TIME) / Convert.ToDouble(LOOP_WAIT_TIME));
                                i = 0;
                                while (i <= iter)
                                {
                                    if (Tracking) { break; }  //if tracking is restored, no need to wait!
                                    Thread.Sleep(LOOP_WAIT_TIME);
                                    i++;
                                }
                                
                                MiscResources.MovingPrimary = false;
                                //Per ASCOM standard, SHOULD be incorporating SlewSettleTime --> but if mount sets tracking, no need to wait!
                                if (!MiscResources.MovingSecondary) //If both primary and secondary are now stopped, restore tracking to what it was.
                                {
                                    Slewing = false;
                                    Tracking = MiscResources.TrackSetFollower;
                                }
                                LogMessage("MoveAxis", "Primary Axis Stop Movement");
                                /*if (!MiscResources.MovingSecondary) //Both primary and secondary are now stopped, start slew settle counter
                                {
                                    //Slewing = false;
                                    MiscResources.SlewSettleStart = DateTime.Now;
                                }
                                */
                                break;
                            case ComparisonResult.Greater:
                                tl.LogMessage("MoveAxis", "Move East");
                                if (MiscResources.MovingPrimary)
                                {
                                    Commander(":Qe#", true, 0);// before mount will change axis speed/direction, expects a stop command
                                    Commander(":Qw#", true, 0);

                                    //and motor must actually stop (~2 secs or Tracking restored)  Maintain synchronous implementation here!
                                    tl.LogMessage("MoveAxis", "Movement finished, waiting for " + MOVEAXIS_WAIT_TIME.ToString() + " ms or until tracking restarts");
                                    iter = Convert.ToInt32(Convert.ToDouble(MOVEAXIS_WAIT_TIME) / Convert.ToDouble(LOOP_WAIT_TIME));
                                    i = 0;
                                    while (i <= iter)
                                    {
                                        if (Tracking) { break; }  //if tracking is restored, no need to wait!
                                        Thread.Sleep(LOOP_WAIT_TIME);
                                        i++;
                                    }

                                }
                                Commander(":Me#", true, 0);
                                //:Me# Move Telescope East at current slew rate
                                //Returns: Nothing
                                MiscResources.MovingPrimary = true;
                                Slewing = true;
                                break;
                            case ComparisonResult.Lower:
                                tl.LogMessage("MoveAxis", "Move West");
                                if (MiscResources.MovingPrimary)
                                {
                                    Commander(":Qe#", true, 0);// before mount will change axis speed/direction, expects a stop command
                                    Commander(":Qw#", true, 0);

                                    //and motor must actually stop (~2 secs or Tracking restored)
                                    tl.LogMessage("MoveAxis", "Movement finished, waiting for " + MOVEAXIS_WAIT_TIME.ToString() + " ms or until tracking restarts");
                                    iter = Convert.ToInt32(Convert.ToDouble(MOVEAXIS_WAIT_TIME) / Convert.ToDouble(LOOP_WAIT_TIME));
                                    i = 0;
                                    while (i <= iter)
                                    {
                                        if (Tracking) { break; }  //if tracking is restored, no need to wait!
                                        Thread.Sleep(LOOP_WAIT_TIME);
                                        i++;
                                    }

                                }
                                Commander(":Mw#", true, 0);
                                //:Mw# Move Telescope West at current slew rate
                                //Returns: Nothing
                                MiscResources.MovingPrimary = true;
                                Slewing = true;
                                break;
                        }
                        break;

                    case TelescopeAxes.axisSecondary:
                        switch (Rate.Compare(0))
                        {
                            case ComparisonResult.Equals:
                                tl.LogMessage("MoveAxis", "Secondary Axis Stop Movement");
                                Commander(":Qn#", true, 0);
                                //:Qn# Halt northward Slews
                                //Returns: Nothing
                                Commander(":Qs#", true, 0);
                                //:Qs# Halt southward Slews
                                //Returns: Nothing

                                //Redo this implementation for async operation.  Add an initial check to verify moving axis state and whether it should be timed out or not
                                iter = Convert.ToInt32(Convert.ToDouble(MOVEAXIS_WAIT_TIME) / Convert.ToDouble(LOOP_WAIT_TIME));
                                i = 0;
                                while (i <= iter)
                                {
                                    if (Tracking) { break; }  //if tracking is restored, no need to wait!
                                    Thread.Sleep(LOOP_WAIT_TIME);
                                    i++;
                                }
                                Slewing = false;  //Should slewing be made false here, or do we need to wait until both primary and secondary are not moving?
                                MiscResources.MovingSecondary = false;


                                /*
                                if (!MiscResources.MovingPrimary) //Both primary and secondary are now stopped, start slew settle counter
                                {
                                    //Slewing = false;
                                    MiscResources.SlewSettleStart = DateTime.Now;
                                }
                                //Per ASCOM standard, SHOULD be incorporating SlewSettleTime
                                */
                                tl.LogMessage("MoveAxis", "Secondary Axis Stop Movement");
                                if (!MiscResources.MovingPrimary) //If both primary and secondary are now stopped, restore tracking to what it was.
                                {
                                    Slewing = false;  //Should slewing be made false here, or do we need to wait until both primary and secondary are not moving?
                                    Tracking = MiscResources.TrackSetFollower;
                                   
                                }

                                break;
                            case ComparisonResult.Greater:
                                tl.LogMessage("MoveAxis", "Move North");
                                if (MiscResources.MovingSecondary)
                                {
                                    Commander(":Qn#", true, 0);// before mount will change axis speed/direction, expects a stop command
                                    Commander(":Qs#", true, 0);

                                    //and motor must actually stop (~2 secs or Tracking restored)  Maintain synchronous implementation here!
                                    tl.LogMessage("MoveAxis", "Movement finished, waiting for " + MOVEAXIS_WAIT_TIME.ToString() + " ms or until tracking restarts");
                                    iter = Convert.ToInt32(Convert.ToDouble(MOVEAXIS_WAIT_TIME) / Convert.ToDouble(LOOP_WAIT_TIME));
                                    i = 0;
                                    while (i <= iter)
                                    {
                                        if (Tracking) { break; }  //if tracking is restored, no need to wait!
                                        Thread.Sleep(LOOP_WAIT_TIME);
                                        i++;
                                    }

                                }
                                Commander(":Mn#", true, 0);
                                //:Mn# Move Telescope North at current slew rate
                                //Returns: Nothing
                                MiscResources.MovingSecondary = true;
                                Slewing = true;
                                break;
                            case ComparisonResult.Lower:
                                tl.LogMessage("MoveAxis", "Move South");
                                if (MiscResources.MovingSecondary)
                                {
                                    Commander(":Qn#", true, 0);// before mount will change axis speed/direction, expects a stop command
                                    Commander(":Qs#", true, 0);

                                    //and motor must actually stop (~2 secs or Tracking restored)  Maintain synchronous implementation here!
                                    tl.LogMessage("MoveAxis", "Movement finished, waiting for " + MOVEAXIS_WAIT_TIME.ToString() + " ms or until tracking restarts");
                                    iter = Convert.ToInt32(Convert.ToDouble(MOVEAXIS_WAIT_TIME) / Convert.ToDouble(LOOP_WAIT_TIME));
                                    i = 0;
                                    while (i <= iter)
                                    {
                                        if (Tracking) { break; }  //if tracking is restored, no need to wait!
                                        Thread.Sleep(LOOP_WAIT_TIME);
                                        i++;
                                    }

                                }
                                Commander(":Ms#", true, 0);
                                //:Ms# Move Telescope South at current slew rate
                                //Returns: Nothing
                                MiscResources.MovingSecondary = true;
                                Slewing = true;
                                break;
                        }

                        break;
                    default:
                        throw new InvalidValueException("Cannot move this axis.");
                }
            }
            catch (Exception ex)
            {
                LogMessage("MoveAxis", $"Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Move the telescope to its park position, stop all motion (or restrict to a small safe range), and set <see cref="AtPark" /> to True.
        /// </summary>
        internal static void Park()
        {
            try
            {
                CheckConnected("Park");
                LogMessage("Park", "Parking Mount");
                if (!AtPark)
                {
                    Commander(":hP#", true, 0);
                    AtPark = true;
                    LogMessage("Park", "Mount is Parked");
                }
                else
                {
                    LogMessage("Park", $"AtPark is {AtPark}");
                    LogMessage("Park", "Ignoring Park command");
                }
            }
            catch (Exception ex)
            {
                LogMessage("Park", $"Error: {ex.Message}");
                throw;
            }
        }

        internal static (int, int) RaDecToAltAz(double deltara, double deltadec)
        {
            int dur1;
            int dur2;

            /*
            double RA = RightAscension * Math.PI / 12; //rad
            double Dec = Declination * Math.PI / 180;  //rad
            double LST = SiderealTime * Math.PI / 12;  //rad
            double lat = SiteLatitude * Math.PI / 180;  //rad

            double h = (LST - RA); //convert to radians
            if (h < 0) { h += 2 * Math.PI; }
            //if (h > Math.PI) { h -= 2 * Math.PI; }

            Double Az = Math.Atan2(Math.Sin(h), Math.Cos(h) * Math.Sin(lat) - Math.Tan(Dec) * Math.Cos(lat));
            Az = Az - Math.PI;
            if (Az < 0) { Az += 2 * Math.PI; }
            Double alt = Math.Asin(Math.Sin(lat) * Math.Sin(Dec) + Math.Cos(lat) * Math.Cos(Dec) * Math.Cos(h));

            LogMessage("RaDecToAltAz", $"RA: {RA * 12 / Math.PI}; Dec: {Dec * 180 / Math.PI}; LST: {LST * 12 / Math.PI}; lat: {lat * 180 / Math.PI}; h: {h * 12 / Math.PI}; CalcAz: {Az * 180 / Math.PI}; Mount Az: {Azimuth}; Transform Az: {T.AzimuthTopocentric}");
            LogMessage("RaDecToAltAz", $"RA: {RA * 12 / Math.PI}; Dec: {Dec * 180 / Math.PI}; LST: {LST * 12 / Math.PI}; lat: {lat * 180 / Math.PI}; h: {h * 12 / Math.PI}; CalcAlt: {alt * 180 / Math.PI}; Mount Alt: {Altitude}; Transform Alt: {T.ElevationTopocentric}");

            double AzRa = (-Math.Sin(lat) + Math.Cos(lat) * Math.Cos(h) * Math.Tan(Dec)) / (Math.Pow(Math.Sin(lat) * Math.Cos(h) - Math.Cos(lat) * Math.Tan(Dec), 2) + Math.Pow(Math.Sin(h), 2)) * deltara;
            double AzDec = (Math.Sin(h) * Math.Cos(lat)) / ((Math.Pow(Math.Sin(lat) * Math.Cos(h) - Math.Cos(lat) * Math.Tan(Dec), 2) + Math.Pow(Math.Sin(h), 2)) * Math.Pow(Math.Cos(Dec), 2)) * deltadec;
            double altRa = (Math.Sin(h) * Math.Cos(Dec) * Math.Cos(lat)) / Math.Sqrt(1 - Math.Pow(Math.Sin(Dec) * Math.Sin(lat) + Math.Cos(Dec) * Math.Cos(lat) * Math.Cos(h), 2)) * deltara;
            double altDec = (-Math.Sin(Dec) * Math.Cos(lat) * Math.Cos(h) + Math.Sin(lat) * Math.Cos(Dec)) / Math.Sqrt(1 - Math.Pow(Math.Sin(Dec) * Math.Sin(lat) + Math.Cos(Dec) * Math.Cos(lat) * Math.Cos(h), 2)) * deltadec;

            LogMessage("RaDecToAltAz", $"deltara: {deltara}; AltAz(RA): {Math.Sqrt(AzRa * AzRa + altRa * altRa)}; deltadec: {deltadec}; AltAz(dec): {Math.Sqrt(AzDec * AzDec + altDec * altDec)}");

            double deltaAz = -(AzRa + AzDec);
            double deltaalt = altDec + altRa;
            */

            //A better method has turned out to be to use the internal tranform functions
            //The reported RA and DEC of the mount gets rounded to the nearest second, but this _SHOULD_ be good enough to transform the
            //ordered RA/DEC motion into Alt/Az movement commands because we are talking about very small movements.
            //General workflow: calculated expected final RA and Dec values, convert to final Alt and Az values, determine deltas and convert that to pulse lengths in each direction
            //Accurate movement will require a level mount so that Alt and Az axes are along the rot and el mechanical axes.
            double RA0 = T.RATopocentric * 15;
            double Dec0 = T.DECTopocentric;
            double RAf = RA0 + deltara;
            double Decf = Dec0 + deltadec;
            double AZ0 = T.AzimuthTopocentric;
            double Alt0 = T.ElevationTopocentric;
            T.SetTopocentric(RAf / 15, Decf);
            double AZfCalc = T.AzimuthTopocentric;
            double AltfCalc = T.ElevationTopocentric;
            double deltaAz = AZfCalc - AZ0;
            double deltaalt = AltfCalc - Alt0;
            T.SetTopocentric(RA0 / 15, Dec0);

            double guiderate = GuideRateDeclination;
            dur1 = Convert.ToInt32(Math.Round(deltaalt / guiderate * 1000));
            dur2 = Convert.ToInt32(Math.Round(deltaAz / guiderate * 1000));

            LogMessage("RaDecToAltAz", $"deltara: {deltara}; deltadec: {deltadec}; deltaalt: {deltaalt}; deltaAz: {deltaAz}; Dur1: {dur1}; Dur2: {dur2}");

            return (dur1, dur2);
        }

        /// <summary>
        /// Moves the scope in the given direction for the given interval or time at
        /// the rate given by the corresponding guide rate property
        /// </summary>
        /// <param name="Direction">The direction in which the guide-rate motion is to be made</param>
        /// <param name="Duration">The duration of the guide-rate motion (milliseconds)</param>
        internal static void PulseGuide(GuideDirections Direction, int Duration)
        {

            try
            {
                CheckConnected("PulseGuide");
                CheckParked("PulseGuide");
            }
            catch (Exception ex)
            {
                tl.LogMessage("PulseGuide", $"Error performing pulse guide: {ex.Message}");
                throw;
            }

            //Note that it is not clear if TTS-160 responds in body frame or LH frame
            //Further experiments show that TTS-160 responds in the (reverse?) body frame: GuideNorth = motion in -el direction.  Unsure Guide E/W
            try
            {
                bool awesome = profileProperties.PulseGuideEquFrame;
                if (awesome)
                {
                    double deltadec = 0;
                    double deltara = 0;
                    double secdur = Convert.ToDouble(Duration) / 1000;
                    double guiderate = GuideRateDeclination; //deg/sec, = 10 arc-sec/sec

                    tl.LogMessage("PulseGuideAwesome", $"GuideRate: {guiderate}");

                    switch (Direction)
                    {
                        case GuideDirections.guideNorth:
                            deltadec = secdur * guiderate; //in deg
                            break;
                        case GuideDirections.guideSouth:
                            deltadec = (-1) * secdur * guiderate; //in deg
                            break;
                        case GuideDirections.guideEast:
                            deltara = secdur * guiderate; //in deg
                            break;
                        case GuideDirections.guideWest:
                            deltara = (-1) * secdur * guiderate; //in deg
                            break;
                    }

                    LogMessage("PulseGuideAwesome", $"deltadec: {deltadec}");
                    LogMessage("PulseGuideAwesome", $"deltara: {deltara}");

                    T.SiteLatitude = SiteLatitude;
                    T.SiteLongitude = SiteLongitude;
                    T.SiteElevation = SiteElevation;
                    T.SiteTemperature = 20;
                    T.Refraction = false;
                    T.SetTopocentric(RightAscension, Declination);

                    (int dur1, int dur2) = RaDecToAltAz(deltara, deltadec);

                    GuideDirections Dir1 = GuideDirections.guideNorth;
                    GuideDirections Dir2 = GuideDirections.guideEast;

                    LogMessage("RaDecToAltAz", $"The following is data to evaluate the best method for converting the pulses:");
                    double RA0 = T.RATopocentric * 15;
                    double Dec0 = T.DECTopocentric;
                    double RAf = RA0 + deltara;
                    double Decf = Dec0 + deltadec;
                    double AZ0 = T.AzimuthTopocentric;
                    double Alt0 = T.ElevationTopocentric;
                    T.SetTopocentric(RAf / 15, Decf);
                    double AZfCalc = T.AzimuthTopocentric;
                    double AltfCalc = T.ElevationTopocentric;
                    double deltaazcalc = AZfCalc - AZ0;
                    double deltaaltcalc = AltfCalc - Alt0;
                    double deltaalttest = Math.Round(Convert.ToDouble(dur1),4) / 1000.0 * guiderate;
                    double deltaaztest = Math.Round(Convert.ToDouble(dur2),4) / 1000.0 * guiderate;
                    double Altftest = Alt0 + deltaalttest;
                    double AZftest = AZ0 + deltaaztest;
                    T.SetAzimuthElevation(AZftest, Altftest);
                    double RAftest = T.RATopocentric * 15;
                    double Decftest = T.DECTopocentric;
                    double deltaratest = RAftest - RA0;
                    double deltadectest = Decftest - Dec0;

                    double delta = Math.Abs(deltadec + deltara);

                    /*
                    LogMessage("RaDecToAltAz:", $"Direction: {Direction}; delta: {utilities.DegreesToDMS(delta,":",":",":",4)}");
                    LogMessage("RaDecToAltAz", $"RA0: {utilities.DegreesToHMS(RA0, ":", ":", ":", 4)}; RAf: {utilities.DegreesToHMS(RAf,":",":",":", 4)}; RAfTest: {utilities.DegreesToHMS(RAftest,":",":",":",4)}");
                    LogMessage("RaDecToAltAz", $"Dec0: {utilities.DegreesToDMS(Dec0, ":", ":", ":", 4)}; Decf: {utilities.DegreesToDMS(Decf, ":", ":", ":", 4)}; DecfTest: {utilities.DegreesToDMS(Decftest, ":", ":", ":", 4)}");
                    LogMessage("RaDecToAltAz", $"Az0: {utilities.DegreesToDMS(AZ0, ":", ":", ":", 4)}; AzfCalc: {utilities.DegreesToDMS(AZfCalc, ":", ":", ":", 4)}; AzfTest: {utilities.DegreesToDMS(AZftest, ":", ":", ":", 4)}");
                    LogMessage("RaDecToAltAz", $"Alt0: {utilities.DegreesToDMS(Alt0, ":", ":", ":", 4)}; AltfCalc: {utilities.DegreesToDMS(AltfCalc, ":", ":", ":", 4)}; AltfTest: {utilities.DegreesToDMS(Altftest, ":", ":", ":", 4)}");
                    LogMessage("RaDecToAltAz", $"deltara: {utilities.DegreesToDMS(deltara, ":", ":", ":", 4)}; deltaratest: {utilities.DegreesToDMS(deltaratest, ":", ":", ":", 4)}");
                    LogMessage("RaDecToAltAz", $"deltadec: {utilities.DegreesToDMS(deltadec, ":", ":", ":", 4)}; deltadectest: {utilities.DegreesToDMS(deltadectest, ":", ":", ":", 4)}");
                    LogMessage("RaDecToAltAz", $"deltaazcalc: {utilities.DegreesToDMS(deltaazcalc, ":", ":", ":", 4)}; deltaaztest: {utilities.DegreesToDMS(deltaaztest, ":", ":", ":", 4)}");
                    LogMessage("RaDecToAltAz", $"deltaaltcalc: {utilities.DegreesToDMS(deltaaltcalc, ":", ":", ":", 4)}; deltaalttest: {utilities.DegreesToDMS(deltaalttest, ":", ":", ":", 4)}");
                    LogMessage("RaDecToAltAz", $"Evaluation Complete");
                    */

                    if (dur1 < 0)
                    {
                        Dir1 = GuideDirections.guideNorth;
                        dur1 = -dur1;
                    }
                    else { Dir1 = GuideDirections.guideSouth; }

                    if (dur2 < 0)
                    {
                        Dir2 = GuideDirections.guideWest;
                        dur2 = -dur2;
                    }
                    else { Dir2 = GuideDirections.guideEast; }

                    LogMessage("PulseGuideAwesome", $"Dir1: {Dir1}");
                    LogMessage("PulseGuideAwesome", $"Dur1: {dur1}");
                    LogMessage("PulseGuideAwesome", $"Dir2: {Dir2}");
                    LogMessage("PulseGuideAwesome", $"Dur2: {dur2}");

                    double curRA = RightAscension;
                    double curDec = Declination;

                    if (dur1 > 0)
                    {
                        PulseGuideAwesome(Dir1, dur1);
                    }
                    if (dur2 > 0)
                    {
                        PulseGuideAwesome(Dir2, dur2);
                    }

                    /*
                    //double SIDEREAL_SECONDS_TO_SI_SECONDS = 0.99726956631945;
                    //deltara = deltara / (15 * SIDEREAL_SECONDS_TO_SI_SECONDS);  //convert to hours                   
                    Thread.Sleep(1000);
                    double RAfact = RightAscension * 15;
                    double Decfact = Declination;
                    double deltaraact = RAfact - RA0;
                    double deltadecact = Decfact - Dec0;
                    LogMessage("PulseGuideAwesome", $"Initial RA: {utilities.DegreesToHMS(RA0, ":", ":", ":", 4)}; Initial Dec: {utilities.DegreesToDMS(Dec0, ":", ":", ":", 4)}");
                    LogMessage("PulseGuideAwesome", $"  Final RA: {utilities.DegreesToHMS(RAfact, ":", ":", ":", 4)};   Final Dec: {utilities.DegreesToDMS(Decfact, ":", ":", ":", 4)}");
                    LogMessage("PulseGuideAwesome", $"   Goal RA: {utilities.DegreesToHMS(RAf, ":", ":", ":", 4)};    Goal Dec: {utilities.DegreesToDMS(Decf, ":", ":", ":", 4)}");
                    LogMessage("PulseGuideAwesome", $"     Delta RA: {utilities.DegreesToHMS(deltaraact, ":", ":", ":", 4)};      Delta Dec: {utilities.DegreesToDMS(deltadecact, ":", ":", ":", 4)}");
                    LogMessage("PulseGuideAwesome", $"Goal Delta RA: {utilities.DegreesToHMS(deltara, ":", ":", ":", 4)}; Goal Delta Dec: {utilities.DegreesToDMS(deltadec, ":", ":", ":", 4)}");
                    */

                    return;

                }
            }
            catch (Exception ex)
            {
                LogMessage("PulseGuideAwesome", $"Error performing awesome pulse guide: {ex.Message}");
                throw;
            }

            LogMessage("PulseGuide", $"pulse guide direction {Direction} duration {Duration}");
            try
            {

                if (MiscResources.IsSlewingToTarget) { throw new InvalidOperationException("Unable to PulseGuide while slewing to target."); }
                if (Duration > 9999) { throw new InvalidValueException("Duration greater than 9999 msec"); }
                if (Duration < 0) { throw new InvalidValueException("Duration less than 0 msec"); }

                if (MiscResources.MovingPrimary &&
                    (Direction == GuideDirections.guideEast || Direction == GuideDirections.guideWest))
                    throw new InvalidOperationException("Unable to PulseGuide while moving same axis.");

                if (MiscResources.MovingSecondary &&
                    (Direction == GuideDirections.guideNorth || Direction == GuideDirections.guideSouth))
                    throw new InvalidOperationException("Unable to PulseGuide while moving same axis.");

                if (IsPulseGuiding)
                    switch (Direction)
                    {
                        case GuideDirections.guideNorth:
                        case GuideDirections.guideSouth:
                            if (MiscResources.NSPulseGuideFlag) { throw new InvalidOperationException("Already PulseGuiding on NS axis, please wait"); }
                            break;
                        case GuideDirections.guideEast:
                        case GuideDirections.guideWest:
                            if (MiscResources.EWPulseGuideFlag) { throw new InvalidOperationException("Already PulseGuiding on EW axis, please wait"); }
                            break;

                    }

                //Check to see if GuideComp is enabled, then correct pulse length if required
                int maxcomp = profileProperties.GuideCompMaxDelta; //set maximum allowable compensation time in msec (PHD2 is 1 sec)
                int bufftime = profileProperties.GuideCompBuffer; //set buffer time to decrement from max in msec to prevent tripping PHD2 limit
                double maxalt = 89; //Sufficiently close to 90 to allow exceeding maxcomp while preventing divide by zero

                if (profileProperties.GuideComp == 1)
                {
                    switch (Direction)
                    {
                        case GuideDirections.guideEast:
                        case GuideDirections.guideWest:

                            tl.LogMessage("PulseGuideComp", "Applying Altitude Compensation");
                            double alt = Altitude;

                            if (alt > maxalt) { alt = maxalt; }; //Prevent receiving divide by zero by limiting altitude to <90 deg

                            double altrad = alt * Math.PI / 180; //convert to radians
                            int compDuration = (int)Math.Round(Duration / Math.Cos(altrad)); //calculate compensated duration
                            tl.LogMessage("PulseGuideComp", "Altitude: " + alt.ToString() + " deg (" + altrad.ToString() + " rad)");
                            tl.LogMessage("PulseGuideComp", "Compensated Time: " + compDuration.ToString("D4"));

                            if (compDuration > (Duration + maxcomp)) //verify we do not exceed maximum time value
                            {
                                compDuration = Duration + maxcomp - bufftime; //clip compensated time to maximum time value (with some buffer)
                                tl.LogMessage("PulseGuideComp", "Compensated Time exceeds maximum: " + (Duration + maxcomp).ToString("D4"));
                                tl.LogMessage("PulseGuideComp", "Setting compensated time to: " + compDuration.ToString("D4"));
                            }
                            Duration = compDuration; //Compensated time is verified good, replace the ordered Duration
                            break;

                    }

                }

                IsPulseGuiding = true;
                LogMessage("PulseGuide", "Guiding with Pulse Guide command");
                switch (Direction)
                {
                    case GuideDirections.guideEast:
                        var guidecmde = ":Mge" + Duration.ToString("D4") + "#";
                        LogMessage("GuideEast", guidecmde);
                        //CommandBlind(guidecmde, true);
                        Commander(guidecmde, true, 0);
                        MiscResources.EWPulseGuideFlag = true;
                        MiscResources.EWPulseGuideFinish = DateTime.Now.AddMilliseconds(Duration);
                        //Thread.Sleep(Duration);
                        break;
                    case GuideDirections.guideNorth:
                        var guidecmdn = ":Mgs" + Duration.ToString("D4") + "#";  //North and south are reversed...
                        LogMessage("GuideNorth", guidecmdn);
                        //CommandBlind(guidecmdn, true);
                        Commander(guidecmdn, true, 0);
                        MiscResources.NSPulseGuideFlag = true;
                        MiscResources.NSPulseGuideFinish = DateTime.Now.AddMilliseconds(Duration);
                        //Thread.Sleep(Duration);
                        break;
                    case GuideDirections.guideSouth:
                        var guidecmds = ":Mgn" + Duration.ToString("D4") + "#";  //North and south are reversed...
                        LogMessage("GuideSouth", guidecmds);
                        //CommandBlind(guidecmds, true);
                        Commander(guidecmds, true, 0);
                        MiscResources.NSPulseGuideFlag = true;
                        MiscResources.NSPulseGuideFinish = DateTime.Now.AddMilliseconds(Duration);
                        //Thread.Sleep(Duration);
                        break;
                    case GuideDirections.guideWest:
                        var guidecmdw = ":Mgw" + Duration.ToString("D4") + "#";
                        LogMessage("GuideWest", guidecmdw);
                        //CommandBlind(guidecmdw, true);
                        Commander(guidecmdw, true, 0);
                        MiscResources.EWPulseGuideFlag = true;
                        MiscResources.EWPulseGuideFinish = DateTime.Now.AddMilliseconds(Duration);
                        //Thread.Sleep(Duration);
                        break;
                }

                LogMessage("PulseGuide", "pulse guide command complete");

            }
            catch (Exception ex)
            {
                LogMessage("PulseGuide", $"Error performing pulse guide: {ex.Message}");
                throw;
            }
        }

        internal static void PulseGuideAwesome(GuideDirections Direction, int Duration)
        {

            //Note that it is not clear if TTS-160 responds in body frame or LH frame
            //Further experiments show that TTS-160 responds in the (reverse?) body frame: GuideNorth = motion in -el direction.  Unsure GuideE/W

            LogMessage("PulseGuideAwesome", $"pulse guide direction {Direction} duration {Duration}");
            try
            {
                //TODO Need to check for valid direction.  Or not, should be verified because it is enumerated
                CheckConnected("PulseGuideAwesome");
                CheckParked("PulseGuideAwesome");

                if (MiscResources.IsSlewingToTarget) { throw new InvalidOperationException("Unable to PulseGuide while slewing to target."); }
                if (Duration > 9999) { throw new InvalidValueException("Duration greater than 9999 msec"); }
                if (Duration < 0) { throw new InvalidValueException("Duration less than 0 msec"); }

                if (MiscResources.MovingPrimary &&
                    (Direction == GuideDirections.guideEast || Direction == GuideDirections.guideWest))
                    throw new InvalidOperationException("Unable to PulseGuide while moving same axis.");

                if (MiscResources.MovingSecondary &&
                    (Direction == GuideDirections.guideNorth || Direction == GuideDirections.guideSouth))
                    throw new InvalidOperationException("Unable to PulseGuide while moving same axis.");

                if (IsPulseGuiding)
                    switch (Direction)
                    {
                        case GuideDirections.guideNorth:
                        case GuideDirections.guideSouth:
                            if (MiscResources.NSPulseGuideFlag) { throw new InvalidOperationException("Already PulseGuiding on NS axis, please wait"); }
                            break;
                        case GuideDirections.guideEast:
                        case GuideDirections.guideWest:
                            if (MiscResources.EWPulseGuideFlag) { throw new InvalidOperationException("Already PulseGuiding on EW axis, please wait"); }
                            break;

                    }

                /*  GuideComp should not be executed when Topocentric Equatorial guiding is enabled
                //Check to see if GuideComp is enabled, then correct pulse length if required
                int maxcomp = profileProperties.GuideCompMaxDelta; //set maximum allowable compensation time in msec (PHD2 is 1 sec)
                int bufftime = profileProperties.GuideCompBuffer; //set buffer time to decrement from max in msec to prevent tripping PHD2 limit
                double maxalt = 89; //Sufficiently close to 90 to allow exceeding maxcomp while preventing divide by zero

                if (profileProperties.GuideComp == 1)
                {
                    switch (Direction)
                    {
                        case GuideDirections.guideEast:
                        case GuideDirections.guideWest:

                            tl.LogMessage("PulseGuideComp", "Applying Altitude Compensation");
                            double alt = Altitude;

                            if (alt > maxalt) { alt = maxalt; }; //Prevent receiving divide by zero by limiting altitude to <90 deg

                            double altrad = alt * Math.PI / 180; //convert to radians
                            int compDuration = (int)Math.Round(Duration / Math.Cos(altrad)); //calculate compensated duration
                            tl.LogMessage("PulseGuideComp", "Altitude: " + alt.ToString() + " deg (" + altrad.ToString() + " rad)");
                            tl.LogMessage("PulseGuideComp", "Compensated Time: " + compDuration.ToString("D4"));

                            if (compDuration > (Duration + maxcomp)) //verify we do not exceed maximum time value
                            {
                                compDuration = Duration + maxcomp - bufftime; //clip compensated time to maximum time value (with some buffer)
                                tl.LogMessage("PulseGuideComp", "Compensated Time exceeds maximum: " + (Duration + maxcomp).ToString("D4"));
                                tl.LogMessage("PulseGuideComp", "Setting compensated time to: " + compDuration.ToString("D4"));
                            }
                            Duration = compDuration; //Compensated time is verified good, replace the ordered Duration
                            break;

                    }

                }
                */

                IsPulseGuiding = true;
                LogMessage("PulseGuide", "Guiding with Pulse Guide command");
                switch (Direction)
                {
                    case GuideDirections.guideEast:
                        var guidecmde = ":Mgw" + Duration.ToString("D4") + "#";  //Maybe 180 out in Az...
                        LogMessage("GuideEast", guidecmde);
                        //CommandBlind(guidecmde, true);
                        Commander(guidecmde, true, 0);
                        MiscResources.EWPulseGuideFlag = true;
                        MiscResources.EWPulseGuideFinish = DateTime.Now.AddMilliseconds(Duration);
                        //Thread.Sleep(Duration);
                        break;
                    case GuideDirections.guideNorth:
                        var guidecmdn = ":Mgs" + Duration.ToString("D4") + "#";  //North and south are switched...
                        LogMessage("GuideNorth", guidecmdn);
                        //CommandBlind(guidecmdn, true);
                        Commander(guidecmdn, true, 0);
                        MiscResources.NSPulseGuideFlag = true;
                        MiscResources.NSPulseGuideFinish = DateTime.Now.AddMilliseconds(Duration);
                        //Thread.Sleep(Duration);
                        break;
                    case GuideDirections.guideSouth:
                        var guidecmds = ":Mgn" + Duration.ToString("D4") + "#";  //North and south are switched...
                        LogMessage("GuideSouth", guidecmds);
                        //CommandBlind(guidecmds, true);
                        Commander(guidecmds, true, 0);
                        MiscResources.NSPulseGuideFlag = true;
                        MiscResources.NSPulseGuideFinish = DateTime.Now.AddMilliseconds(Duration);
                        //Thread.Sleep(Duration);
                        break;
                    case GuideDirections.guideWest:
                        var guidecmdw = ":Mge" + Duration.ToString("D4") + "#";  //Maybe 180 out in Az
                        LogMessage("GuideWest", guidecmdw);
                        //CommandBlind(guidecmdw, true);
                        Commander(guidecmdw, true, 0);
                        MiscResources.EWPulseGuideFlag = true;
                        MiscResources.EWPulseGuideFinish = DateTime.Now.AddMilliseconds(Duration);
                        //Thread.Sleep(Duration);
                        break;
                }

                LogMessage("PulseGuideAwesome", "pulse guide command complete");

            }
            catch (Exception ex)
            {
                LogMessage("PulseGuideAwesome", $"Error performing pulse guide Awesome: {ex.Message}");

                throw;
            }
        }

        /// <summary>
        /// The right ascension (hours) of the telescope's current equatorial coordinates,
        /// in the coordinate system given by the EquatorialSystem property
        /// </summary>
        internal static double RightAscension
        {
            get
            {
                try
                {
                    CheckConnected("Right Ascension Get");

                    //var result = CommandString(":GR#", true);
                    var result = Commander(":GR#", true, 2);
                    //:GR# Get telescope Right Ascension
                    //Returns: HH:MM.T# or HH:MM:SS#
                    //The current telescope Right Ascension depending on the selected precision.

                    double rightAscension = utilities.HMSToHours(result);

                    tl.LogMessage("Right Ascension", "Get - " + utilities.HoursToHMS(rightAscension, ":", ":"));
                    return rightAscension;
                }
                catch (Exception ex)
                {
                    tl.LogMessage("Right Ascension Get", $"Error: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// The right ascension tracking rate offset from sidereal (seconds per sidereal second, default = 0.0)
        /// </summary>
        internal static double RightAscensionRate
        {
            get
            {
                //RightAscensionRate is not implemented by TTS-160, return 0.0
                double rightAscensionRate = 0.0;
                LogMessage("RightAscensionRate get", $"{rightAscensionRate}");
                return rightAscensionRate;
            }
            set
            {
                LogMessage("RightAscensionRate Set", "Not implemented");
                throw new PropertyNotImplementedException("RightAscensionRate", true);
            }
        }

        /// <summary>
        /// Sets the telescope's park position to be its current position.
        /// </summary>
        internal static void SetPark()
        {
            tl.LogMessage("SetPark", "Not implemented");
            throw new MethodNotImplementedException("SetPark");
        }

        internal static PierSide CalculateSideOfPier(double rightAscension)
        {
            double hourAngle = astroUtilities.ConditionHA(SiderealTime - rightAscension);

            var destinationSOP = hourAngle > 0
                ? PierSide.pierEast
                : PierSide.pierWest;
            return destinationSOP;
        }

        /// <summary>
        /// Indicates the pointing state of the mount. Read the articles installed with the ASCOM Developer
        /// Components for more detailed information.
        /// </summary>
        internal static PierSide SideOfPier
        {
            get
            {
                //tl.LogMessage("SideOfPier Get", "Not implemented");
                //throw new PropertyNotImplementedException("SideOfPier", false);
                var pierSide = CalculateSideOfPier(RightAscension);

                LogMessage("SideOfPier", "Get - " + pierSide);
                return pierSide;
            }
            set
            {
                tl.LogMessage("SideOfPier Set", "Not implemented");
                throw new PropertyNotImplementedException("SideOfPier", true);
            }
        }

        /// <summary>
        /// The local apparent sidereal time from the telescope's internal clock (hours, sidereal)
        /// </summary>
        internal static double SiderealTime
        {
            get
            {
                try
                {
                    CheckConnected("SiderealTime");
                    var result = Commander(":GS#", true, 2).TrimEnd('#');
                    double siderealTime = utilities.HMSToHours(result);
                    double siteLongitude = SiteLongitude;
                    tl.LogMessage("SiderealTime", "Get GMST - " + siderealTime.ToString());
                    siderealTime += siteLongitude / 360.0 * 24.0;
                    siderealTime = astroUtilities.ConditionRA(siderealTime);
                    tl.LogMessage("SiderealTime", "Local Sidereal - " + siderealTime.ToString());
                    return siderealTime;
                }
                catch (Exception ex)
                {
                    tl.LogMessage("Sidereal Time", $"Error: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// The elevation above mean sea level (meters) of the site at which the telescope is located
        /// </summary>
        internal static double SiteElevation
        {
            get
            {
                return profileProperties.SiteElevation;
            }
            set
            {
                try
                {
                    if ((value < -300) || (value > 10000)) { throw new ASCOM.InvalidValueException($"Invalid Site Elevation ${value}"); }
                    profileProperties.SiteElevation = value;
                    WriteProfile(profileProperties);
                }
                catch (Exception ex)
                {
                    LogMessage("Site Altitude set", $"Error: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// The geodetic(map) latitude (degrees, positive North, WGS84) of the site at which the telescope is located.
        /// </summary>
        internal static double SiteLatitude
        {
            get
            {
                try
                {

                    LogMessage("SiteLatitude get", "Getting Site Latitude");
                    CheckConnected("SiteLatitude get");

                    if (profileProperties.DriverSiteOverride)
                    {
                        return profileProperties.DriverSiteLatitude;
                    }
                    else
                    {

                        var result = Commander(":Gt#", true, 2);
                        //:Gt# Get Site Latitude
                        //Returns: sDD*MM#

                        double siteLatitude = utilities.DMSToDegrees(result);

                        LogMessage("SiteLatitude get", utilities.DegreesToDMS(siteLatitude, ":", ":",""));
                        return siteLatitude;

                    }
                }
                catch (Exception ex)
                {
                    LogMessage("SiteLatitude get", $"Error: {ex.Message}");
                    throw;
                }
            }
            set
            {
                LogMessage("SiteLatitude set", "Not implemented");
                throw new PropertyNotImplementedException("SiteLatitude", true);
            }
        }

        internal static double SiteLatitudeInit
        {
            get
            {
                try
                {

                    LogMessage("SiteLatitudeInit get", "Getting Site Latitude from the mount");
                    CheckConnected("SiteLatitudeInit get");

                    var result = Commander(":Gt#", true, 2);
                    //:Gt# Get Site Latitude
                    //Returns: sDD*MM#

                    double siteLatitude = utilities.DMSToDegrees(result);

                    LogMessage("SiteLatitudeInit get", utilities.DegreesToDMS(siteLatitude, ":", ":",""));
                    return siteLatitude;

                }
                catch (Exception ex)
                {
                    LogMessage("SiteLatitudeInit get", $"Error: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// The longitude (degrees, positive East, WGS84) of the site at which the telescope is located.
        /// </summary>
        internal static double SiteLongitude
        {
            get
            {
                LogMessage("SiteLongitude get", "Getting Site Longitude");
                try
                {

                    LogMessage("SiteLongitude get", "Getting Site Longitude");
                    CheckConnected("SiteLongitude get");

                    if (profileProperties.DriverSiteOverride)
                    {
                        return profileProperties.DriverSiteLongitude;
                    }
                    else
                    {
                        //var result = CommandString(":Gg#", true);
                        var result = Commander(":Gg#", true, 2);
                        //:Gg# Get Site Longitude
                        //Returns: sDDD*MM#, east negative

                        //New firmware is now West Negative when entering in handpad, reports as east negative still
                        double siteLongitude = -1 * utilities.DMSToDegrees(result); //correct to West negative
                                                                                    //double siteLongitude = utilities.DMSToDegrees(result);

                        LogMessage("SiteLongitude get", utilities.DegreesToDMS(siteLongitude, ":", ":",""));
                        return siteLongitude;
                    }

                }
                catch (Exception ex)
                {
                    LogMessage("SiteLongitude get", $"Error: {ex.Message}");
                    throw;
                }
            }
            set
            {
                LogMessage("SiteLongitude set", "Not implemented");
                throw new PropertyNotImplementedException("SiteLongitude", true);
            }
        }

        internal static double SiteLongitudeInit
        {
            get
            {
                LogMessage("SiteLongitudeInit get", "Getting Site Longitude");
                try
                {

                    LogMessage("SiteLongitudeInit get", "Getting Site Longitude from the mount");
                    CheckConnected("SiteLongitudeInit get");

                    //var result = CommandString(":Gg#", true);
                    var result = Commander(":Gg#", true, 2);
                    //:Gg# Get Site Longitude
                    //Returns: sDDD*MM#, east negative

                    //New firmware is now West Negative when entering in handpad, reports as east negative still
                    double siteLongitude = -1 * utilities.DMSToDegrees(result); //correct to West negative
                    //double siteLongitude = utilities.DMSToDegrees(result);
                    LogMessage("SiteLongitudeInit get", utilities.DegreesToDMS(siteLongitude, ":", ":",""));
                    return siteLongitude;

                }
                catch (Exception ex)
                {
                    LogMessage("siteLongitudeInit get", $"Error: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Specifies a post-slew settling time (sec.).
        /// </summary>
        internal static short SlewSettleTime
        {
            get
            {
                return profileProperties.SlewSettleTime;
            }
            set
            {
                try
                {
                    if (value >= 0)
                    {
                        profileProperties.SlewSettleTime = value;
                        WriteProfile(profileProperties);
                    }
                    else { throw new InvalidValueException("Settle Time must be >= 0"); }
                }
                catch (Exception ex)
                {
                    LogMessage("SlewSettleTime Set", $"Error:{ex.Message}");

                    throw;
                }
            }
        }

        /// <summary>
        /// Move the telescope to the given local horizontal coordinates
        /// This method must be implemented if <see cref="CanSlewAltAz" /> returns True.
        /// It does not return until the slew is complete.
        /// </summary>
        internal static void SlewToAltAz(double Azimuth, double Altitude)
        {
            LogMessage("SlewToAltAz", "Not implemented");
            throw new MethodNotImplementedException("SlewToAltAz");
            /*
            try
            {
                CheckConnected("SlewToAltAz");
                CheckParked("SlewToAltAz");
                //if (AtPark) { throw new ASCOM.ParkedException("Cannot SlewToAltAz while mount is parked"); }
                if (Tracking) { throw new ASCOM.InvalidOperationException("Cannot SlewToAltAz while Tracking"); }

                if ((Azimuth < 0) || (Azimuth > 360)) { throw new ASCOM.InvalidValueException($"Invalid Azimuth ${Azimuth}"); }
                if ((Altitude < 0) || (Altitude > 90)) { throw new ASCOM.InvalidValueException($"Invalid Altitude ${Altitude}"); }

                tl.LogMessage("SlewToAltAz", "Az: " + Azimuth.ToString() + "; Alt: " + Altitude.ToString());

                //Convert AltAz to RaDec Topocentric
                
                T.SiteLatitude = SiteLatitude;
                T.SiteLongitude = SiteLongitude;
                T.SiteElevation = SiteElevation;
                T.SiteTemperature = 20;
                T.Refraction = false;
                T.SetAzimuthElevation(Azimuth, Altitude);

                double curtargDec = 0;
                double curtargRA = 0;
                try
                {
                    curtargDec = TargetDeclination;
                }
                catch
                {
                    curtargDec = 0;
                }
                try
                {
                    curtargRA = TargetRightAscension;
                }
                catch
                {
                    curtargRA = 0;
                }

                MiscResources.SlewAltAzTrackOverride = true;
                tl.LogMessage("SlewToAltAz", "Calling SlewToCoordinates, track override enabled");
                tl.LogMessage("SlewToAltAz", "Az: " + Azimuth.ToString() + "; Alt: " + Altitude.ToString());
                tl.LogMessage("SlewToAltAz", "Derived Ra: " + utilities.HoursToHMS(T.RATopocentric, ":", ":") + "; Derived Dec: " + utilities.DegreesToDMS(T.DECTopocentric, ":", ":"));
                SlewToCoordinates(T.RATopocentric, T.DECTopocentric);
                MiscResources.SlewAltAzTrackOverride = false;
                tl.LogMessage("SlewToAltAz", "Track override disabled");

                TargetDeclination = curtargDec;
                TargetRightAscension = curtargRA;

            }
            catch (Exception ex)
            {
                tl.LogMessage("SlewToAltAz", $"Error: {ex.Message}");
                throw;
            }
            */
        }

        /// <summary>
        /// Move the telescope to the given local horizontal coordinates.
        /// This method must be implemented if <see cref="CanSlewAltAzAsync" /> returns True.
        /// It returns immediately, with <see cref="Slewing" /> set to True
        /// </summary>
        /// <param name="Azimuth">Azimuth to which to move</param>
        /// <param name="Altitude">Altitude to which to move to</param>
        internal static void SlewToAltAzAsync(double Azimuth, double Altitude)
        {
            LogMessage("SlewToAltAzAsync", "Not implemented");
            throw new MethodNotImplementedException("SlewToAltAzAsync");
            /*
            try
            {
                CheckConnected("SlewToAltAzAsync");
                CheckParked("SlewToAltAzAsync");
                //if (AtPark) { throw new ASCOM.ParkedException("Cannot SlewToAltAzAsync while mount is parked"); }
                if (Tracking) { throw new ASCOM.InvalidOperationException("Cannot SlewToAltAzAsync while Tracking"); }
                
                if ((Azimuth < 0) || (Azimuth > 360)) { throw new ASCOM.InvalidValueException($"Invalid Azimuth ${Azimuth}"); }
                if ((Altitude < 0) || (Altitude > 90)) { throw new ASCOM.InvalidValueException($"Invalid Altitude ${Altitude}"); }

                tl.LogMessage("SlewToAltAzAsync", "Az: " + Azimuth.ToString() + "; Alt: " + Altitude.ToString());

                T.SiteLatitude = SiteLatitude;
                T.SiteLongitude = SiteLongitude;
                T.SiteElevation = SiteElevation;
                T.SiteTemperature = 20;
                T.Refraction = false;
                T.SetAzimuthElevation(Azimuth, Altitude);

                double curtargDec = 0;
                double curtargRA = 0;
                try
                {
                    curtargDec = TargetDeclination;
                }
                catch
                {
                    curtargDec = 0;
                }
                try
                {
                    curtargRA = TargetRightAscension;
                }
                catch
                {
                    curtargRA = 0;
                }

                MiscResources.SlewAltAzTrackOverride = true;
                tl.LogMessage("SlewToAltAzAsync", "Calling SlewToCoordinatesAsync, track override enabled");
                tl.LogMessage("SlewToAltAzAsync", "Az: " + Azimuth.ToString() + "; Alt: " + Altitude.ToString());
                tl.LogMessage("SlewToAltAzAsync", "Derived Ra: " + utilities.HoursToHMS(T.RATopocentric, ":", ":") + "; Derived Dec: " + utilities.DegreesToDMS(T.DECTopocentric, ":", ":"));
                SlewToCoordinatesAsync(T.RATopocentric, T.DECTopocentric);
                MiscResources.SlewAltAzTrackOverride = false;
                tl.LogMessage("SlewToAltAzAsync", "Track override disabled");

                TargetDeclination = curtargDec;
                TargetRightAscension = curtargRA;
            }
            catch (Exception ex)
            {
                tl.LogMessage("SlewToAltAzAsync", $"Error: {ex.Message}");
                throw;
            }
            */
        }

        /// <summary>
        /// Move the telescope to the given equatorial coordinates.  
        /// This method must be implemented if <see cref="CanSlew" /> returns True.
        /// It does not return until the slew is complete.
        /// </summary>
        internal static void SlewToCoordinates(double RightAscension, double Declination)
        {
            LogMessage("SlewToCoordinates", "Setting Coordinates as Target and Slewing");
            try
            {
                CheckConnected("SlewToCoordinates");
                CheckParked("SlewToCoordinates");

                if (!Tracking && !MiscResources.SlewAltAzTrackOverride) { throw new ASCOM.InvalidOperationException("Cannot SlewToCoordinates while not Tracking"); }

                if ((Declination >= -90) && (Declination <= 90))
                {
                    TargetDeclination = Declination;
                }
                else
                {
                    throw new ASCOM.InvalidValueException($"Invalid Declination: {Declination}");
                }
                if ((RightAscension >= 0) && (RightAscension <= 24))
                {
                    TargetRightAscension = RightAscension;
                }
                else
                {
                    throw new ASCOM.InvalidValueException($"Invalid Right Ascension: {RightAscension}");
                }

                SlewToTarget();
            }
            catch (Exception ex)
            {
                LogMessage("SlewToCoordinates", $"Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Move the telescope to the given equatorial coordinates.
        /// This method must be implemented if <see cref="CanSlewAsync" /> returns True.
        /// It returns immediately, with <see cref="Slewing" /> set to True
        /// </summary>
        internal static void SlewToCoordinatesAsync(double RightAscension, double Declination)
        {
            LogMessage("SlewToCoordinatesAsync", "Setting Coordinates as Target and Slewing");
            try
            {
                CheckConnected("SlewToCoordinatesAsync");
                CheckParked("SlewToCoordinatesAsync");

                if (!Tracking && !MiscResources.SlewAltAzTrackOverride) { throw new ASCOM.InvalidOperationException("Cannot SlewToCoordinatesAsync while not Tracking"); }

                if ((Declination >= -90) && (Declination <= 90))
                {
                    TargetDeclination = Declination;
                }
                else
                {
                    throw new ASCOM.InvalidValueException($"Invalid Declination: {Declination}");
                }
                if ((RightAscension >= 0) && (RightAscension <= 24))
                {
                    TargetRightAscension = RightAscension;
                }
                else
                {
                    throw new ASCOM.InvalidValueException($"Invalid Right Ascension: {RightAscension}");
                }
                LogMessage("SlewToCoordinatesAsync", "Starting Async Slew: RA - " + RightAscension.ToString() + "; Dec - " + Declination.ToString());
                SlewToTargetAsync();
                LogMessage("SlewToCoordinatesAsync", "Slew Commenced");
            }
            catch (Exception ex)
            {
                LogMessage("SlewToCoordinatesAsync", $"Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Move the telescope to the <see cref="TargetRightAscension" /> and <see cref="TargetDeclination" /> coordinates.
        /// This method must be implemented if <see cref="CanSlew" /> returns True.
        /// It does not return until the slew is complete.
        /// </summary>

        internal static void SlewToTarget()
        {
            LogMessage("SlewToTarget", "Slewing To Target");

            try
            {
                if (!MiscResources.IsTargetSet) { throw new Exception("Target Not Set"); }
                CheckConnected("SlewToTarget");
                CheckParked("SlewToTarget");

                if (!Tracking && !MiscResources.SlewAltAzTrackOverride) { throw new ASCOM.InvalidOperationException("Cannot SlewToTarget while not Tracking"); }

                if (MiscResources.IsSlewingToTarget) //Are we currently in a GoTo?
                {
                    throw new InvalidOperationException("Error: GoTo In Progress");
                }

                bool wasTracking = Tracking;

                double TargRA = MiscResources.Target.RightAscension;
                double TargDec = MiscResources.Target.Declination;
                //Assume Target is valid due to setting checks

                //bool result = CommandBool(":MS#", true);
                bool result = bool.Parse(Commander(":MS#", true, 1));
                if (result) { throw new Exception("Unable to slew:" + result + " Object Below Horizon"); }

                Slewing = true;
                MiscResources.IsSlewingToTarget = true;

                //If we were tracking before, TTS-160 will stop tracking on commencement of slew
                //then resume tracking when slew complete.  If TTS-160 was NOT tracking, we need to
                //implement a loop to check on slew status using mount position rates

                if (wasTracking)
                {
                    int counter = 0;
                    int RateLimit = 200; //Wait time between queries, in msec
                    int TimeLimit = 300; //How long to wait for slew to finish before throwing error, in sec
                    while (!Tracking)
                    {
                        utilities.WaitForMilliseconds(200); //limit asking rate to 0.2 Hz
                        counter++;
                        if (counter > TimeLimit * 1000 / RateLimit)
                        {
                            AbortSlew();
                            throw new ASCOM.DriverException("SlewToTarget Failed: Timeout");
                        }
                    }
                    Thread.Sleep(SlewSettleTime * 1000);
                    Slewing = false;
                    MiscResources.IsSlewingToTarget = false;
                    return;
                }
                else
                {
                    //Create loop to monitor slewing and return when done
                    utilities.WaitForMilliseconds(500); //give motors time to start
                    double resid = 1000; //some number greater than .0001 (~0.5/3600)
                    double targresid = 1000;
                    double threshold = 0.5 / 3600; //0.5 second accuracy
                    double targthreshold = 10; //start checking w/in 10 seconds of target
                    int inc = 3; //3 readings <.0001 to determine at target
                    int faultinc = 300; //Long term check used to check for no movement
                    int interval = 100; //100 msec between readings
                    double RAold = RightAscension;
                    double Decold = Declination;

                    while (inc >= 0)
                    {
                        utilities.WaitForMilliseconds(interval); //let the mount move a bit
                        double RAnew = RightAscension;
                        double Decnew = Declination;
                        double RADelt = RAnew - RAold;
                        double DecDelt = Decnew - Decold;
                        double RADeltTarg = RAnew - TargRA;
                        double DecDeltTarg = Decnew - TargDec;
                        resid = Math.Sqrt(Math.Pow(RADelt, 2) + Math.Pow(DecDelt, 2)); //need to convert this to alt/az rather than RA/Dec
                        targresid = Math.Sqrt(Math.Pow(RADeltTarg, 2) + Math.Pow(DecDeltTarg, 2));
                        if ((resid <= threshold) & (targresid <= targthreshold))
                        {
                            switch (inc)  //We are good, decrement the count
                            {
                                case 0:
                                    utilities.WaitForMilliseconds(SlewSettleTime * 1000);
                                    Slewing = false;
                                    MiscResources.IsSlewingToTarget = false;
                                    return;
                                default:
                                    inc--;
                                    break;
                            }
                        }
                        else
                        {
                            switch (inc)  //We are bad, increment the count up to 3
                            {
                                case 2:
                                    inc++;
                                    break;
                                case 1:
                                    inc++;
                                    break;
                                case 0:
                                    inc++;
                                    break;
                            }
                        }

                        if (resid <= threshold)
                        {
                            switch (faultinc)  //No motion detected, decrement the count
                            {
                                case 0:
                                    Slewing = false;
                                    MiscResources.IsSlewingToTarget = false;
                                    Commander(":Q#", true, 0);
                                    throw new ASCOM.DriverException("SlewToTarget Failed");
                                default:
                                    faultinc--;
                                    break;
                            }
                        }
                        else
                        {
                            //Motion detected, increment back up
                            if (faultinc < 300)
                            {
                                faultinc++;
                            }
                        }

                        RAold = RAnew;
                        Decold = Decnew;
                        RAnew = 0;
                        Decnew = 0;

                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage("SlewToTarget", $"Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Move the telescope to the <see cref="TargetRightAscension" /> and <see cref="TargetDeclination" />  coordinates.
        /// This method must be implemented if <see cref="CanSlewAsync" /> returns True.
        /// It returns immediately, with <see cref="Slewing" /> set to True
        /// </summary>
        internal static void SlewToTargetAsync()
        {

            tl.LogMessage("SlewToTargetAsync", "Slewing To Target");

            try
            {
                if (!MiscResources.IsTargetSet) { throw new ASCOM.ValueNotSetException("Target Not Set"); }
                CheckConnected("SlewToTargetAsync");
                CheckParked("SlewToTargetAsync");

                if (!Tracking && !MiscResources.SlewAltAzTrackOverride) { throw new ASCOM.InvalidOperationException("Cannot SlewToTargetAsync while not Tracking"); }

                if (MiscResources.IsSlewingToTarget) //Are we currently in a GoTo?
                {
                    throw new ASCOM.InvalidOperationException("Error: GoTo In Progress");
                }


                //bool result = CommandBool(":MS#", true);
                bool result = bool.Parse(Commander(":MS#", true, 1));
                if (result) { throw new ASCOM.InvalidOperationException("Unable to slew: target below horizon"); }  //Need to review other implementation
                MiscResources.SlewTarget.RightAscension = MiscResources.Target.RightAscension;
                MiscResources.SlewTarget.Declination = MiscResources.Target.Declination;
                Slewing = true;
                MiscResources.IsSlewingToTarget = true;  //Might be redundant...
                MiscResources.IsSlewingAsync = true;

            }
            catch (Exception ex)
            {
                LogMessage("SlewToTargetAsync", $"Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// True if telescope is in the process of moving in response to one of the
        /// Slew methods or the <see cref="MoveAxis" /> method, False at all other times.
        /// </summary>
        internal static bool Slewing
        {
            //'Slewing' query (:D#) is not implemented in TTS-160, keep track in driver.
            get
            {
                try
                {
                    
                    CheckConnected("Slewing");
                    LogMessage("Slewing - Get", "Getting Slew Status");
                    //Catching the end of an async slew event
                    //TODO - Add error checking to see if we actually ended where we wanted
                    if (MiscResources.IsSlewing && MiscResources.IsSlewingAsync && Tracking)  //If doing a slew (IsSlewingAsync = true), need an additional error check at end to confirm mount is ok.
                    {
                        if ((SlewSettleTime > 0) && (MiscResources.SlewSettleStart == DateTime.MinValue))
                        {
                            MiscResources.SlewSettleStart = DateTime.Now;
                            LogMessage("Slewing Status", $"{false}; Commencing Slew Settling");
                            return MiscResources.IsSlewing;
                        }
                        else if ((SlewSettleTime > 0) && (MiscResources.SlewSettleStart > DateTime.MinValue))
                        {
                            TimeSpan ts = DateTime.Now.Subtract(MiscResources.SlewSettleStart);
                            if (ts.TotalSeconds >= SlewSettleTime)
                            {
                                LogMessage("Slewing Status", $"{false}; slew settle complete");
                                MiscResources.IsSlewing = false;
                                MiscResources.IsSlewingToTarget = false;
                                MiscResources.IsSlewingAsync = false;
                                MiscResources.SlewSettleStart = DateTime.MinValue;

                                //Check that final location is within 1' of target, else throw an error
                                double RA = RightAscension * 15;
                                double Dec = Declination;
                                double errdist = Math.Sqrt(Math.Pow(RA - MiscResources.SlewTarget.RightAscension * 15, 2) + Math.Pow(Dec - MiscResources.SlewTarget.Declination, 2)) / 60; //distance in minutes
                                LogMessage("Slewing get", $"Slew error distance: {errdist} arcmin, Tolerance: 1 arcmin");

                                if (errdist > 1.0) //See if errdistance is outside of tolerance
                                {
                                    string targRA = utilities.HoursToHMS(MiscResources.SlewTarget.RightAscension);
                                    string targDec = utilities.DegreesToDMS(MiscResources.SlewTarget.Declination);
                                    string actRA = utilities.HoursToHMS(RA / 15);
                                    string actDec = utilities.DegreesToDMS(Dec);
                                    LogMessage("Slewing get", $"Slew result outside tolerance. Target Ra: {targRA}; Actual RA: {actRA}; Target Dec: {targDec}; Actual Dec: {actDec}; Error Distance: {errdist} arcmin");
                                    throw new DriverException($"Slew result outside tolerance. Target Ra: {targRA}; Actual RA: {actRA}; Target Dec: {targDec}; Actual Dec: {actDec}; Error Distance: {errdist} arcmin.  Check to ensure mount is operating correctly");
                                    
                                }
                                return false;
                            }
                            else
                            {
                                LogMessage("Slewing Status", $"{true}; Slew Settling in progress");
                                return MiscResources.IsSlewing;
                            }

                        }
                        else if (SlewSettleTime == 0)
                        {
                            LogMessage("Slewing Status", $"{false}");
                            MiscResources.IsSlewing = false;
                            MiscResources.IsSlewingToTarget = false;
                            MiscResources.IsSlewingAsync = false;
                            MiscResources.SlewSettleStart = DateTime.MinValue;

                            //Check that final location is within 1' of target, else throw an error
                            double RA = RightAscension * 15;
                            double Dec = Declination;
                            double errdist = Math.Sqrt(Math.Pow(RA - MiscResources.Target.RightAscension * 15, 2) + Math.Pow(Dec - MiscResources.Target.Declination, 2)) / 60; //distance in minutes
                            LogMessage("Slewing get", $"Slew error distance: {errdist} arcmin, Tolerance: 1 arcmin");

                            if (errdist > 1.0) //See if errdistance is outside of tolerance
                            {
                                string targRA = utilities.HoursToHMS(MiscResources.Target.RightAscension);
                                string targDec = utilities.DegreesToDMS(MiscResources.Target.Declination);
                                string actRA = utilities.HoursToHMS(RA / 15);
                                string actDec = utilities.DegreesToDMS(Dec);
                                LogMessage("Slewing get", $"Slew result outside tolerance. Target Ra: {targRA}; Actual RA: {actRA}; Target Dec: {targDec}; Actual Dec: {actDec}; Error Distance: {errdist} arcmin");
                                throw new DriverException($"Slew result outside tolerance. Target Ra: {targRA}; Actual RA: {actRA}; Target Dec: {targDec}; Actual Dec: {actDec}; Error Distance: {errdist} arcmin.  Check to ensure mount is operating correctly");
                            }
                            return false;
                            
                        }
                        else
                        {
                            LogMessage("Slewing get", "Unknown condition while checking slew status, please verify hardware is operating correctly");
                            throw new DriverException("Unknown condition while checking slew status, please verify hardware is operating correctly");
                        }
                    }
                    /*else if (!MiscResources.IsSlewingAsync && MiscResources.IsSlewing && Tracking)
                    {
                        if ((SlewSettleTime > 0) && (MiscResources.SlewSettleStart == DateTime.MinValue))
                        {
                            MiscResources.SlewSettleStart = DateTime.Now;
                            LogMessage("Slewing Status", $"{false}; Commencing Slew Settling");
                            return MiscResources.IsSlewing;
                        }
                        else if ((SlewSettleTime > 0) && (MiscResources.SlewSettleStart > DateTime.MinValue))
                        {
                            TimeSpan ts = DateTime.Now.Subtract(MiscResources.SlewSettleStart);
                            if (ts.TotalSeconds >= SlewSettleTime)
                            {
                                LogMessage("Slewing Status", $"{false}; Slew Settling Complete");
                                MiscResources.IsSlewing = false;
                                MiscResources.SlewSettleStart = DateTime.MinValue;
                                return false;
                            }
                            else
                            {
                                LogMessage("Slewing Status", $"{true}; Slew Settling in progress");
                                return MiscResources.IsSlewing;
                            }
                        }
                        if (SlewSettleTime == 0)
                        {
                            LogMessage("Slewing Status", $"{false}");
                            MiscResources.IsSlewing = false;
                            MiscResources.SlewSettleStart = DateTime.MinValue;
                            return false;
                        }
                        else
                        {
                            LogMessage("Slewing get", "Unknown condition while checking slew status, please verify hardware is operating correctly");
                            throw new DriverException("Unknown condition while checking slew status, please verify hardware is operating correctly");
                        }

                    }*/
                    else
                    {
                        LogMessage("Slewing Status", MiscResources.IsSlewing.ToString());
                        return MiscResources.IsSlewing;
                    }


                }
                catch (Exception ex)
                {
                    LogMessage("Slewing", $"Error: {ex.Message}");
                    throw;
                }

            }
            set
            {
                MiscResources.IsSlewing = value;
            }
        }

        /// <summary>
        /// Matches the scope's local horizontal coordinates to the given local horizontal coordinates.
        /// </summary>
        internal static void SyncToAltAz(double TAzimuth, double TAltitude)
        {
            try
            {
                CheckConnected("SyncToAltAz");
                CheckParked("SyncToAltAz");

                if (Tracking) { throw new ASCOM.InvalidOperationException("Cannot SyncToAltAz while Tracking"); }

                if ((TAzimuth < 0) || (TAzimuth > 360)) { throw new ASCOM.InvalidValueException($"Invalid Azimuth ${TAzimuth}"); }
                if ((TAltitude < 0) || (TAltitude > 90)) { throw new ASCOM.InvalidValueException($"Invalid Altitude ${TAltitude}"); }

                T.SiteLatitude = SiteLatitude;
                T.SiteLongitude = SiteLongitude;
                T.SiteElevation = SiteElevation;
                T.SiteTemperature = 20;
                T.Refraction = false;
                T.SetAzimuthElevation(TAzimuth, TAltitude);

                SyncToCoordinates(T.RATopocentric, T.DECTopocentric);
                LogMessage("SyncToAltAz", "Complete");
                LogMessage("SyncToAltAz", $"Target: Az: {TAzimuth}; Alt: {TAltitude}");
                LogMessage("SyncToAltAz", $"Current: Az: {Azimuth}; Alt: {Altitude}");
            }
            catch (Exception ex)
            {
                LogMessage("SyncToAltAz", $"Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Matches the scope's equatorial coordinates to the given equatorial coordinates.
        /// </summary>
        internal static void SyncToCoordinates(double TRightAscension, double TDeclination)
        {
            tl.LogMessage("SyncToCoordinates", "Setting Coordinates as Target and Syncing");
            try
            {
                CheckConnected("SyncToCoordinates");
                CheckParked("SyncToCoordinates");

                //TODO Tracking control is not implemented in TTS-160, no point in checking it <---TODO: It now is, FIX THIS?!

                if ((TDeclination >= -90) && (TDeclination <= 90))
                {
                    TargetDeclination = TDeclination;
                }
                else
                {
                    throw new ASCOM.InvalidValueException($"Invalid Declination: {TDeclination}");
                }
                if ((TRightAscension >= 0) && (TRightAscension <= 24))
                {
                    TargetRightAscension = TRightAscension;
                }
                else
                {
                    throw new ASCOM.InvalidValueException($"Invalid Right Ascension: {RightAscension}");
                }

                LogMessage("SyncToCoordinates", "PreSync: Ra - " + utilities.HoursToHMS(RightAscension, ":", ":","") + "; Dec - " + utilities.DegreesToDMS(Declination, ":", ":",""));
                var ret = Commander(":CM#", true, 2);
                LogMessage("SyncToCoordinates", "Complete: " + ret);
                //Thread.Sleep(100);
                LogMessage("SyncToCoordinates", $"Target: Ra: " + utilities.HoursToHMS(TRightAscension, ":", ":","") + "; Dec: " + utilities.DegreesToDMS(TDeclination, ":", ":",""));
                LogMessage("SyncToCoordinates", $"Current: Ra: " + utilities.HoursToHMS(RightAscension, ":", ":","") + "; Dec: " + utilities.DegreesToDMS(Declination, ":", ":",""));

            }
            catch (Exception ex)
            {
                tl.LogMessage("SyncToCoordinates", $"Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Matches the scope's equatorial coordinates to the target equatorial coordinates.
        /// </summary>
        internal static void SyncToTarget()
        {
            tl.LogMessage("SyncToTarget", "Syncing to Target");
            try
            {
                if (!MiscResources.IsTargetSet) { throw new Exception("Target not set"); }
                CheckConnected("SyncToTarget");
                CheckParked("SyncToTarget");

                //TODO Tracking control is not implemented in TTS-160, no point in checking it.....IT NOW IS, TODO FIX IT!

                LogMessage("SyncToTarget", "PreSync: Ra - " + utilities.HoursToHMS(RightAscension, ":", ":","") + "; Dec - " + utilities.DegreesToDMS(Declination, ":", ":",""));
                //For some reason TTS-160 returns a message and not catching it causes
                var ret = Commander(":CM#", true, 2);  //further commands to act funny (results are 1 order off despite the
                                                       //buffer clears)
                LogMessage("SyncToTarget", "Complete: " + ret);
                //Thread.Sleep(100);
                LogMessage("SyncToTarget", $"Assumed Target: Ra: " + utilities.HoursToHMS(TargetRightAscension, ":", ":","") + "; Dec: " + utilities.DegreesToDMS(TargetDeclination, ":", ":",""));
                LogMessage("SyncToTarget", $"Current: Ra: " + utilities.HoursToHMS(RightAscension, ":", ":","") + "; Dec: " + utilities.DegreesToDMS(Declination, ":", ":",""));
            }
            catch (Exception ex)
            {
                LogMessage("SyncToTarget", $"Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// The declination (degrees, positive North) for the target of an equatorial slew or sync operation
        /// </summary>
        internal static double TargetDeclination
        {

            get
            {
                //Not implemented in TTS-160, simulated in driver
                try
                {
                    CheckConnected("TargetDeclination");
                    if (MiscResources.IsTargetDecSet)
                    {
                        LogMessage("TargetDeclination get", "Target Dec Get to:" + MiscResources.Target.Declination);
                        return MiscResources.Target.Declination;
                    }
                    else
                    {
                        throw new ASCOM.InvalidOperationException("Target Declination Not Set");
                    }
                }
                catch (Exception ex)
                {
                    LogMessage("Target Declination get", $"Error: {ex.Message}");
                    throw;
                }
            }

            set
            {
                try
                {
                    LogMessage("TargetDeclination set", "Setting Target Dec");
                    CheckConnected("TargetDeclination");
                    if (value < -90)
                    {
                        throw new ASCOM.InvalidValueException("Target Declination < -90 deg");
                    }
                    else if (value > 90)
                    {
                        throw new ASCOM.InvalidValueException("Target Declination > 90 deg");
                    }
                    else
                    {
                        var targDec = utilities.DegreesToDMS(value, "*", ":");
                        bool result = false;
                        if (value >= 0)
                        {
                            result = bool.Parse(Commander($":Sd+{targDec}#", true, 1));
                        }
                        else
                        {
                            result = bool.Parse(Commander($":Sd{targDec}#", true, 1));
                        }

                        if (!result) { throw new ASCOM.InvalidValueException("Invalid Target Declination:" + targDec); }

                        LogMessage("TargetDeclination Set", "Target Dec Set to: " + targDec);
                        MiscResources.Target.Declination = value;
                        if (!MiscResources.IsTargetDecSet)
                        {
                            MiscResources.IsTargetDecSet = true;
                        }

                        if (MiscResources.IsTargetRASet & !MiscResources.IsTargetSet)
                        {
                            MiscResources.IsTargetSet = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogMessage("TargetDeclination Set", $"Error: {ex.Message}");
                    throw;
                }

            }
        }

        /// <summary>
        /// The right ascension (hours) for the target of an equatorial slew or sync operation
        /// </summary>
        internal static double TargetRightAscension
        {
            get
            {
                //Not implemented in TTS-160, simulated in driver
                try
                {
                    CheckConnected("TargetRightAscension");
                    if (MiscResources.IsTargetRASet)
                    {
                        tl.LogMessage("TargetRightAscension get", "Target RA Get to:" + MiscResources.Target.RightAscension);
                        return MiscResources.Target.RightAscension;
                    }
                    else
                    {
                        throw new ASCOM.InvalidOperationException("Target Right Ascension not set");
                    }
                }
                catch (Exception ex)
                {
                    LogMessage("Target Right Ascension get", $"Error: {ex.Message}");
                    throw;
                }
            }
            set
            {
                LogMessage("TargetRightAscension set", "Setting Target RA");
                try
                {
                    CheckConnected("Set Target RA");
                    if (value < 0)
                    {
                        throw new ASCOM.InvalidValueException("Target RA < 0h");
                    }
                    else if (value > 24)
                    {
                        throw new ASCOM.InvalidValueException("Target RA > 24h");
                    }
                    else
                    {
                        string targRA = utilities.HoursToHMS(value, ":", ":");
                        bool result = bool.Parse(Commander(":Sr" + targRA + "#", true, 1));

                        if (!result) { throw new ASCOM.InvalidValueException("Invalid Target Right Ascension:" + targRA); }

                        LogMessage("TargetRightAscension set", "Target RA Set to:" + targRA);
                        MiscResources.Target.RightAscension = value;
                        if (!MiscResources.IsTargetRASet)
                        {
                            MiscResources.IsTargetRASet = true;
                        }

                        if (MiscResources.IsTargetDecSet & !MiscResources.IsTargetSet)
                        {
                            MiscResources.IsTargetSet = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogMessage("TargetRightAscension set", $"Error: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// The state of the telescope's sidereal tracking drive.
        /// </summary>
        internal static bool Tracking
        {
            get
            {
                try
                {
                    CheckConnected("GetTracking");
                    LogMessage("Tracking get", "Retrieving Tracking Status");
                    var ret = Commander(":GW#", true, 2);
                    bool tracking = (ret[1] == 'T');
                    LogMessage("Tracking get", $"{tracking}");
                    return tracking;
                }
                catch (Exception ex)
                {
                    LogMessage("Tracking Get", $"Error: {ex.Message}");
                    throw;
                }

            }
            set
            {

                try
                {
                    CheckConnected("SetTracking");
                    CheckSlewing("SetTracking");

                    LogMessage("Tracking set", $"Set Tracking Enabled to: {value}");
                    if (value) { Commander(":T1#", true, 0); }
                    else if (!value) { Commander(":T0#", true, 0); }
                    else { throw new ASCOM.InvalidValueException($"Expected True or False, received: {value}"); }
                    MiscResources.TrackSetFollower = value;
                }
                catch (Exception ex)
                {
                    LogMessage("Tracking set", $"Error: {ex.Message}");
                    throw;
                }

            }
        }

        /// <summary>
        /// The current tracking rate of the telescope's sidereal drive
        /// </summary>
        internal static DriveRates TrackingRate
        {
            get
            {
                try
                {
                    CheckConnected("TrackingRate Get");

                    LogMessage("TrackingRate Get", $"{MiscResources.TrackingRateCurrent}");
                    return MiscResources.TrackingRateCurrent;
                }
                catch (Exception ex)
                {
                    LogMessage("TrackingRate Get", $"Error: {ex.Message}");
                    throw;
                }

            }
            set
            {

                try
                {

                    CheckConnected("SetTrackingRate");
                    LogMessage("TrackingRate Set", "Setting Tracking Rate: " + value.ToString());
                    switch (value)
                    {
                        case DriveRates.driveSidereal:
                            Commander(":TQ#", true, 0);
                            break;

                        case DriveRates.driveLunar:
                            Commander(":TL#", true, 0);
                            break;

                        case DriveRates.driveSolar:
                            Commander(":TS#", true, 0);
                            break;

                        default:
                            throw new ASCOM.InvalidValueException("Invalid Rate: " + value.ToString());

                    }
                    MiscResources.TrackingRateCurrent = value;
                    LogMessage("TrackingRate Set", $"Tracking Rate Set To: {value}");
                }
                catch (Exception ex)
                {
                    LogMessage("TrackingRate Set", $"Error: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Returns a collection of supported <see cref="DriveRates" /> values that describe the permissible
        /// values of the <see cref="TrackingRate" /> property for this telescope type.
        /// </summary>
        internal static ITrackingRates TrackingRates
        {
            get
            {
                ITrackingRates trackingRates = new TrackingRates();
                LogMessage("TrackingRates get", "Getting TrackingRates");
                foreach (DriveRates driveRate in trackingRates)
                {
                    LogMessage("TrackingRates get", $"{driveRate}");
                }
                return trackingRates;
            }
        }

        /// <summary>
        /// The UTC date/time of the telescope's internal clock
        /// </summary>
        internal static DateTime UTCDate
        {
            //Can set local date/time, NOT UTC per TTS-160 implementation
            //This call pulls local and converts to UTC based off of UTC value
            get
            {
                try
                {
                    CheckConnected("UTCDateGet");


                    var localdate = Commander(":GC#", true, 2);
                    var localtime = Commander(":GL#", true, 2);
                    var utcoffset = Commander(":GG#", true, 2);

                    int mo = Int32.Parse(localdate.Substring(0, 2));
                    int da = Int32.Parse(localdate.Substring(3, 2));
                    int yr = Int32.Parse(localdate.Substring(6, 2)) + 2000;

                    int hh = Int32.Parse(localtime.Substring(0, 2));
                    int mm = Int32.Parse(localtime.Substring(3, 2));
                    int ss = Int32.Parse(localtime.Substring(6, 2));

                    double utcoffsetnum = double.Parse(utcoffset.TrimEnd('#'));

                    DateTime lcl = new DateTime(yr, mo, da, hh, mm, ss, DateTimeKind.Local);
                    DateTime utcDate = lcl.AddHours(utcoffsetnum);

                    //DateTime utcDate = DateTime.UtcNow;
                    LogMessage("UTCDate", "Get - " + utcDate.ToString("MM/dd/yy HH:mm:ss")); // String.Format("MM/dd/yy HH:mm:ss", utcDate));
                    return utcDate;
                }
                catch (Exception ex)
                {
                    LogMessage("UTCDate Get", $"Error: {ex.Message}.");
                    throw;
                }

            }
            set
            {
                //Need to determine if there are any values we need to verify
                //For now, assume that C# will verify a DateTime value is passed
                //Does not appear to be required to do this per ASCOM standards...
                try
                {
                    CheckConnected("UTCDateSet");

                    //This converts the provided UTC value to local and updates TTS-160
                    //var utcoffset = CommandString(":GG#", true);
                    var utcoffset = Commander(":GG#", true, 2);
                    double utcoffsetnum = double.Parse(utcoffset.TrimEnd('#'));
                    DateTime localdatetime = value.AddHours((-1) * utcoffsetnum);

                    string newdate = localdatetime.ToString("MM/dd/yy");
                    //string res = CommandString(":SC" + newdate + "#", true);
                    string res = Commander(":SC" + newdate + "#", true, 2);
                    bool resBool = char.GetNumericValue(res[0]) == 1;
                    if (!resBool) { throw new ASCOM.InvalidValueException("UTC Date Set Invalid Date: " + newdate); }

                    string newtime = localdatetime.ToString("HH:mm:ss");
                    //resBool = CommandBool(":SL" + newtime + "#", true);
                    resBool = bool.Parse(Commander(":SL" + newtime + "#", true, 1));
                    //resBool = char.GetNumericValue(res[0]) == 1;
                    LogMessage("UTCDate set", "Issuing a throwaway SiderealTime call to ensure next UTCDate pull provides accurate time");
                    double siderealthrow = SiderealTime; //Firmware bug, ensures the next read handpad time is correct
                    if (!resBool) { throw new ASCOM.InvalidValueException("UTC Date Set Invalid Time: " + newtime); }
                }
                catch (Exception ex)
                {
                    LogMessage("UTCDate Set", $"Error: {ex.Message}");
                    throw;
                }

            }
        }

        /// <summary>
        /// Takes telescope out of the Parked state.
        /// </summary>
        internal static void Unpark()
        {
            LogMessage("Unpark", "Not implemented");
            throw new MethodNotImplementedException("Unpark");
        }

        #endregion

        #region Private properties and methods
        // here are some useful properties and methods that can be used as required
        // to help with driver development

        /// <summary>
        /// Returns true if there is a valid connection to the driver hardware
        /// </summary>
        private static bool IsConnected
        {
            get
            {
                
                return connectedState;

            }
        }

        /// <summary>
        /// Use this function to throw an exception if we aren't connected to the hardware
        /// </summary>
        /// <param name="message"></param>
        private static void CheckConnected(string message)
        {
            if (!IsConnected)
            {
                throw new ASCOM.NotConnectedException(message);
            }
        }

        /// <summary>
        /// Use this function to throw an exception if we are slewing
        /// </summary>
        /// <param name="message"></param>
        private static void CheckSlewing(string message)
        {
            if (Slewing)
            {
                throw new ASCOM.InvalidOperationException("Unable to " + message + " while slewing");
            }
        }

        /// <summary>
        /// Use this function to throw an exception if we are parked
        /// </summary>
        private static void CheckParked(string message)
        {
            if (AtPark) { throw new ASCOM.ParkedException("Unable to use " + message + " while parked"); }
        }

        /// <summary>
        /// Read the device configuration from the ASCOM Profile store
        /// </summary>
        internal static ProfileProperties ReadProfile()
        {
            lock(LockObject)
            {
                ProfileProperties profileProperties = new ProfileProperties();

                using (Profile driverProfile = new Profile())
                {

                    driverProfile.DeviceType = "Telescope";

                    profileProperties.TraceLogger = Convert.ToBoolean(driverProfile.GetValue(driverID, traceStateProfileName, string.Empty, traceStateDefault));
                    profileProperties.ComPort = driverProfile.GetValue(driverID, comPortProfileName, string.Empty, comPortDefault);
                    profileProperties.SiteElevation = Double.Parse(driverProfile.GetValue(driverID, siteElevationProfileName, string.Empty, siteElevationDefault));
                    profileProperties.SlewSettleTime = Int16.Parse(driverProfile.GetValue(driverID, SlewSettleTimeName, string.Empty, SlewSettleTimeDefault));
                    profileProperties.SiteLatitude = Double.Parse(driverProfile.GetValue(driverID, SiteLatitudeName, string.Empty, SiteLatitudeDefault));
                    profileProperties.SiteLongitude = Double.Parse(driverProfile.GetValue(driverID, SiteLongitudeName, string.Empty, SiteLongitudeDefault));
                    profileProperties.CompatMode = Int32.Parse(driverProfile.GetValue(driverID, CompatModeName, string.Empty, CompatModeDefault));
                    profileProperties.CanSetGuideRatesOverride = Convert.ToBoolean(driverProfile.GetValue(driverID, CanSetGuideRatesOverrideName, string.Empty, CanSetGuideRatesOverrideDefault));
                    profileProperties.SyncTimeOnConnect = Convert.ToBoolean(driverProfile.GetValue(driverID, SyncTimeOnConnectName, string.Empty, SyncTimeOnConnectDefault));
                    profileProperties.GuideComp = Int32.Parse(driverProfile.GetValue(driverID, GuideCompName, string.Empty, GuideCompDefault));
                    profileProperties.GuideCompMaxDelta = Int32.Parse(driverProfile.GetValue(driverID, GuideCompMaxDeltaName, string.Empty, GuideCompMaxDeltaDefault));
                    profileProperties.GuideCompBuffer = Int32.Parse(driverProfile.GetValue(driverID, GuideCompBufferName, string.Empty, GuideCompBufferDefault));
                    profileProperties.TrackingRateOnConnect = Int32.Parse(driverProfile.GetValue(driverID, TrackingRateOnConnectName, string.Empty, TrackingRateOnConnectDefault));
                    profileProperties.PulseGuideEquFrame = Convert.ToBoolean(driverProfile.GetValue(driverID, PulseGuideEquFrameName, string.Empty, PulseGuideEquFrameDefault));
                    profileProperties.DriverSiteOverride = Convert.ToBoolean(driverProfile.GetValue(driverID, DriverSiteOverrideName, string.Empty, DriverSiteOverrideDefault));
                    profileProperties.DriverSiteLatitude = Double.Parse(driverProfile.GetValue(driverID, DriverSiteLatitudeName, string.Empty, DriverSiteLatitudeDefault));
                    profileProperties.DriverSiteLongitude = Double.Parse(driverProfile.GetValue(driverID, DriverSiteLongitudeName, string.Empty, DriverSiteLongitudeDefault));
                    profileProperties.HCGuideRate = Int32.Parse(driverProfile.GetValue(driverID, HCGuideRateName, string.Empty, HCGuideRateDefault));

                }
                return profileProperties;
            }

        }

        /// <summary>
        /// Write the device configuration to the  ASCOM  Profile store
        /// </summary>
        internal static void WriteProfile(ProfileProperties profileProperties)
        {
            lock(LockObject)
            {
                using (Profile driverProfile = new Profile())
                {
                    driverProfile.DeviceType = "Telescope";

                    driverProfile.WriteValue(driverID, traceStateProfileName, profileProperties.TraceLogger.ToString());
                    if (!(profileProperties.ComPort is null)) driverProfile.WriteValue(driverID, comPortProfileName, profileProperties.ComPort);
                    driverProfile.WriteValue(driverID, siteElevationProfileName, profileProperties.SiteElevation.ToString());
                    driverProfile.WriteValue(driverID, SlewSettleTimeName, profileProperties.SlewSettleTime.ToString());
                    driverProfile.WriteValue(driverID, SiteLatitudeName, profileProperties.SiteLatitude.ToString());
                    driverProfile.WriteValue(driverID, SiteLongitudeName, profileProperties.SiteLongitude.ToString());
                    driverProfile.WriteValue(driverID, CompatModeName, profileProperties.CompatMode.ToString());
                    driverProfile.WriteValue(driverID, CanSetGuideRatesOverrideName, profileProperties.CanSetGuideRatesOverride.ToString());
                    driverProfile.WriteValue(driverID, SyncTimeOnConnectName, profileProperties.SyncTimeOnConnect.ToString());
                    driverProfile.WriteValue(driverID, GuideCompName, profileProperties.GuideComp.ToString());
                    driverProfile.WriteValue(driverID, GuideCompMaxDeltaName, profileProperties.GuideCompMaxDelta.ToString());
                    driverProfile.WriteValue(driverID, GuideCompBufferName, profileProperties.GuideCompBuffer.ToString());
                    driverProfile.WriteValue(driverID, TrackingRateOnConnectName, profileProperties.TrackingRateOnConnect.ToString());
                    driverProfile.WriteValue(driverID, PulseGuideEquFrameName, profileProperties.PulseGuideEquFrame.ToString());
                    driverProfile.WriteValue(driverID, DriverSiteOverrideName, profileProperties.DriverSiteOverride.ToString());
                    driverProfile.WriteValue(driverID, DriverSiteLatitudeName, profileProperties.DriverSiteLatitude.ToString());
                    driverProfile.WriteValue(driverID, DriverSiteLongitudeName, profileProperties.DriverSiteLongitude.ToString());
                    driverProfile.WriteValue(driverID, HCGuideRateName, profileProperties.HCGuideRate.ToString());

                }
            }

        }

        /// <summary>
        /// Log helper function that takes identifier and message strings
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="message"></param>
        internal static void LogMessage(string identifier, string message)
        {
            tl.LogMessageCrLf(identifier, message);
        }

        /// <summary>
        /// Log helper function that takes formatted strings and arguments
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>

        internal static void LogMessage(string identifier, string message, params object[] args)
        {
            var msg = string.Format(message, args);
            LogMessage(identifier, msg);
        }
        #endregion
    }
}
