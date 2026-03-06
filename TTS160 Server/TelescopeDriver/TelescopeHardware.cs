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
// Implements:	ASCOM Telescope interface version: 4
// Author:		Reid Smythe <rwsmythe@gmail.com>
//
// Edit Log:
//
// Date			Who	Vers	Description
// -----------	---	-----	-------------------------------------------------------
// 11Aug2024    RWS 355.0.0 Added advanced features included in the 355 firmware.
// 14JUL2024    RWS 354.1.3 Added a short delay to CommandBlind commands to ensure there are no order collisions.
// 23JUN2024    RWS 354.1.2 Removed version from driver name in chooser.  Added slew result checking to indicate possible slew error if mount stops early.
//                          Changed stop move axis routine to by async IAW ASCOM standard
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

using ASCOM.DeviceInterface;
using ASCOM.LocalServer;
using ASCOM.Utilities;
using ASCOM.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using ASCOM.Astrometry.AstroUtils;
using System.Threading.Tasks;
using ASCOM.Common.Helpers;
using Microsoft.VisualBasic;
using System.Linq.Expressions;

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
    /// Shared static hardware class for the TTS-160 Panther telescope mount.
    /// Implements all actual telescope communication using the LX200 serial protocol.
    /// A single serial connection is shared across all COM driver instances
    /// (<see cref="TelescopeDriver"/>), which delegate their ASCOM method calls to this class.
    /// Decorated with <see cref="HardwareClassAttribute"/> so that the ASCOM Local Server
    /// correctly disposes hardware resources on shutdown.
    /// </summary>
    [HardwareClass()]
    internal static class TelescopeHardware
    {
        /// <summary>
        /// Current driver version string for the TTS-160 Panther mount ASCOM driver.
        /// This driver is based on the LX200 serial protocol.
        /// </summary>
        private static readonly string driverVersion = "356.0.0";

        #region Default Profile values

        /// <summary>Profile key for the COM port used for serial communication with the mount.</summary>
        internal static string comPortProfileName = "COM Port";
        /// <summary>Default COM port. Typically overridden by the user in the setup dialog.</summary>
        internal static string comPortDefault = "COM1";

        /// <summary>Profile key for enabling/disabling diagnostic trace logging.</summary>
        internal static string traceStateProfileName = "Trace Level";
        /// <summary>Default trace state. "false" disables logging; "true" enables verbose logging to the ASCOM log directory.</summary>
        internal static string traceStateDefault = "false";

        /// <summary>Profile key for the observer's site elevation in metres above sea level.</summary>
        internal static string siteElevationProfileName = "Site Elevation";
        /// <summary>Default site elevation in metres. Zero assumes sea level.</summary>
        internal static string siteElevationDefault = "0";

        /// <summary>Profile key for the post-slew settling time in seconds.</summary>
        internal static string SlewSettleTimeName = "Slew Settle Time";
        /// <summary>Default settling time (1 second). Allows the mount to stabilize after a slew completes.</summary>
        internal static string SlewSettleTimeDefault = "1";

        /// <summary>Profile key for the site latitude stored on the mount (degrees). Used to detect whether the mount has been configured.</summary>
        internal static string SiteLatitudeName = "Site Latitude";
        /// <summary>Default latitude sentinel value (100). Out-of-range on purpose so the driver knows no valid latitude has been set.</summary>
        internal static string SiteLatitudeDefault = "100";

        /// <summary>Profile key for the site longitude stored on the mount (degrees).</summary>
        internal static string SiteLongitudeName = "Site Longitude";
        /// <summary>Default longitude sentinel value (200). Out-of-range on purpose so the driver knows no valid longitude has been set.</summary>
        internal static string SiteLongitudeDefault = "200";

        /// <summary>Profile key controlling whether the driver synchronizes the mount's clock to the PC clock on connect.</summary>
        internal static string SyncTimeOnConnectName = "Sync Time on Connect";
        /// <summary>Default is "true" — the mount clock is synced to PC time each time the driver connects.</summary>
        internal static string SyncTimeOnConnectDefault = "true";

        /// <summary>Profile key for the guiding compensation mode. Controls azimuth pulse-guide compensation based on target altitude.</summary>
        internal static string GuideCompName = "Guiding Compensation";
        /// <summary>Default guiding compensation mode (0 = disabled).</summary>
        internal static string GuideCompDefault = "0";

        /// <summary>Profile key for the maximum allowed guiding compensation delta in milliseconds.</summary>
        internal static string GuideCompMaxDeltaName = "Guiding Compensation Max Delta";
        /// <summary>Default maximum delta (1000 ms). Caps the azimuth pulse-guide extension to prevent runaway corrections.</summary>
        internal static string GuideCompMaxDeltaDefault = "1000";

        /// <summary>Profile key for the guiding compensation buffer in milliseconds.</summary>
        internal static string GuideCompBufferName = "Guiding Compensation Buffer";
        /// <summary>Default buffer (20 ms). Minimum additional time added to azimuth guide pulses when compensation is active.</summary>
        internal static string GuideCompBufferDefault = "20";

        /// <summary>Profile key for whether pulse-guide commands are issued in the equatorial (topocentric) reference frame.</summary>
        internal static string PulseGuideEquFrameName = "PulseGuide Equatorial Frame";
        /// <summary>Default is "true" — pulse guides use equatorial/topocentric frame rather than alt-az.</summary>
        internal static string PulseGuideEquFrameDefault = "true";

        /// <summary>Profile key for enabling the driver-side site location override (ignores mount-reported lat/lon).</summary>
        internal static string DriverSiteOverrideName = "Driver Site Override";
        /// <summary>Default is "false" — the driver reads site location from the mount.</summary>
        internal static string DriverSiteOverrideDefault = "false";

        /// <summary>Profile key for the driver-override site latitude (degrees, -90 to +90).</summary>
        internal static string DriverSiteLatitudeName = "Driver Site Latitude";
        /// <summary>Default override latitude (0 degrees).</summary>
        internal static string DriverSiteLatitudeDefault = "0";

        /// <summary>Profile key for the driver-override site longitude (degrees, -180 to +180).</summary>
        internal static string DriverSiteLongitudeName = "Driver Site Longitude";
        /// <summary>Default override longitude (0 degrees).</summary>
        internal static string DriverSiteLongitudeDefault = "0";

        /// <summary>Profile key for forcing pulse-guide commands to execute synchronously (blocks until complete).</summary>
        internal static string PulseGuideDurationSynchronousName = "Synchronous PulseGuide Duration";
        /// <summary>Default is "false" — pulse guides run asynchronously so the caller is not blocked.</summary>
        internal static string PulseGuideDurationSynchronousDefault = "false";

        /// <summary>Profile key for enabling align-on-sync mode, which builds a pointing model from sync points.</summary>
        internal static string AlignOnSyncEnabledName = "Align on Sync Mode";
        /// <summary>Default is "false" — standard sync behavior without building a multi-point alignment model.</summary>
        internal static string AlignOnSyncEnabledDefault = "false";

        /// <summary>Profile key for the number of sync points collected for align-on-sync mode.</summary>
        internal static string AlignOnSyncPointsName = "Align on Sync Mode Sync Points";
        /// <summary>Default is "0" — no sync points recorded yet.</summary>
        internal static string AlignOnSyncPointsDefault = "0";

        /// <summary>Profile key for enabling a user-defined park location instead of the default home position.</summary>
        internal static string SetParkLocName = "Set Park Location";
        /// <summary>Default is "false" — the mount parks at its default home position.</summary>
        internal static string SetParkLocDefault = "false";

        /// <summary>Profile key indicating whether the mount is currently in a parked state.</summary>
        internal static string ParkLocName = "Park Location";
        /// <summary>Default is "false" — the mount starts in an unparked state.</summary>
        internal static string ParkLocDefault = "false";

        /// <summary>Profile key for the custom park position altitude (degrees, 0 to 90).</summary>
        internal static string ParkLocAltName = "Park Location Altitude";
        /// <summary>Default park altitude (0 degrees — horizon).</summary>
        internal static string ParkLocAltDefault = "0";

        /// <summary>Profile key for the custom park position azimuth (degrees, 0 to 360).</summary>
        internal static string ParkLocAzName = "Park Location Azimuth";
        /// <summary>Default park azimuth (180 degrees — due south).</summary>
        internal static string ParkLocAzDefault = "180";

        #endregion

        #region Constants

        /// <summary>
        /// Minimum delay in milliseconds between successive MoveAxis commands.
        /// Prevents the mount's serial command buffer from overflowing when rapid
        /// MoveAxis calls are issued by the client application.
        /// </summary>
        internal static int MOVEAXIS_WAIT_TIME = 2000;

        /// <summary>
        /// Delay in milliseconds after a sync command before reading the mount's position.
        /// Gives the mount time to update its internal position registers so subsequent
        /// position queries return the corrected coordinates.
        /// </summary>
        internal static int SYNC_WAIT_TIME = 200;

        /// <summary>
        /// Flag indicating whether the connected mount is running development-level firmware
        /// (version 355 or later). Set at runtime during the connection handshake based on
        /// the firmware version string reported by the mount; enables additional features
        /// available only in newer firmware.
        /// </summary>
        internal static bool DEV_FIRMWARE = false;

        #endregion

        #region Variable Declarations

        /// <summary>ASCOM DeviceID (COM ProgID) for this driver. Set once during the driver's static initializer.</summary>
        private static string DriverProgId = "";

        /// <summary>Human-readable driver description shown in the ASCOM Chooser. Set once during the driver's static initializer.</summary>
        private static string DriverDescription = "";

        /// <summary>
        /// Indicates whether the local server currently has an active serial connection to the mount.
        /// Shared across all COM driver instances; access should be synchronized via <see cref="LockObject"/>.
        /// </summary>
        private static bool connectedState;

        /// <summary>
        /// Guard flag ensuring one-time initialization logic (e.g., firmware detection) executes
        /// only on the first connection and is not repeated on subsequent connects.
        /// </summary>
        private static bool runOnce = false;

        /// <summary>
        /// Indicates that a connect or disconnect operation is currently in progress.
        /// Used to prevent re-entrant connection attempts from multiple COM clients.
        /// </summary>
        private static bool connecting;

        /// <summary>ASCOM Utilities helper. Provides general-purpose utility methods (e.g., time conversions).</summary>
        internal static Util utilities;

        /// <summary>ASCOM AstroUtils instance used for Right Ascension conditioning and related calculations.</summary>
        internal static AstroUtils astroUtils;

        /// <summary>ASCOM AstroUtilities instance providing additional astronomical calculation methods.</summary>
        internal static AstroUtilities astroUtilities;

        /// <summary>
        /// Trace logger for writing diagnostic information to the ASCOM log directory.
        /// Controlled by the "Trace Level" profile setting; shared across all COM driver instances.
        /// </summary>
        internal static Utilities.TraceLogger tl;

        /// <summary>ASCOM Transform instance used to convert between coordinate systems (e.g., J2000 to topocentric).</summary>
        internal static Transform T;

        /// <summary>
        /// Synchronization object used to serialize access to shared state and serial port
        /// communication, preventing concurrent calls from multiple COM driver instances
        /// from corrupting the command/response sequence.
        /// </summary>
        internal static readonly object LockObject = new object();

        /// <summary>In-memory copy of the persisted ASCOM Profile settings. Modified via the setup dialog and applied on connect.</summary>
        internal static ProfileProperties profileProperties = new ProfileProperties();

        /// <summary>
        /// List of GUIDs identifying each connected COM driver instance.
        /// Used to track how many clients are connected so the serial port is only
        /// closed when the last client disconnects.
        /// </summary>
        private static List<Guid> uniqueIds = new List<Guid>();

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="TTS160"/> class.
        /// Must be public for COM registration.
        /// </summary>
        static TelescopeHardware()
        {
            try
            {
                tl = new Utilities.TraceLogger("", $"TTS160.Hardware v. {driverVersion}");

                DriverProgId = Telescope.DriverProgId; // Get this device's ProgID so that it can be used to read the Profile configuration values

                profileProperties = ReadProfile();
                tl.Enabled = profileProperties.TraceLogger;

                T = new Transform();

                LogMessage("Telescope", "Completed start-up");
            }
            catch (Exception ex)
            {
                try { LogMessage("TelescopeHardware", $"Initialization exception: {ex}"); } catch { }
                MessageBox.Show($"{ex.Message}", "Exception creating ASCOM.TTS160.Telescope", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }

        }

        internal static void InitializeHardware()
        {
            // This method will be called every time a new ASCOM client loads your driver
            LogMessage("InitializeHardware", $"Start:");
            //MessageBox.Show("Wait!");
            // Make sure that "one off" activities are only undertaken once
            if (runOnce == false)
            {
                profileProperties = ReadProfile();
                LogMessage("InitializeHardware", $"Starting one-off initialization.");

                DriverDescription = Telescope.DriverDescription; // Get this device's Chooser description

                LogMessage("InitializeHardware", $"ProgID: {DriverProgId}, Description: {DriverDescription}");

                connectedState = false; // Initialise connected to false
                utilities = new Util(); //Initialise ASCOM Utilities object
                astroUtilities = new AstroUtilities(); // Initialise ASCOM Astronomy Utilities object
                astroUtils = new AstroUtils();

                // Add your own "one off" device initialisation here e.g. validating existence of hardware and setting up communications

                Slewing = false;
                MiscResources.IsSlewing = false;
                MiscResources.IsSlewingToTarget = false;
                MiscResources.SlewSettleStart = DateTime.MinValue;
                MiscResources.EWMoveAxisSettleStart = DateTime.MinValue;
                MiscResources.NSMoveAxisSettleStart = DateTime.MinValue;
                MiscResources.IsPulseGuiding = false;
                MiscResources.MovingPrimary = false;
                MiscResources.MovingSecondary = false;
                MiscResources.EWMoveAxisStopFlag = false;
                MiscResources.NSMoveAxisStopFlag = false;

                LogMessage("InitializeHardware", $"One-off initialization complete.");
                runOnce = true; // Set the flag to ensure that this code is not run again
            }
            LogMessage("InitializeHardware", "Completed basic initialization");
        }


        //
        // PUBLIC COM INTERFACE ITelescopeV4 IMPLEMENTATION
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

        /// <summary>
        /// Central serial communication method for the TTS-160 mount. All telescope commands flow through
        /// this method, which delegates to <see cref="SharedResources.SendMessage"/> for actual serial I/O.
        /// Implements the LX200 serial command protocol.
        /// </summary>
        /// <param name="command">
        /// The command string to send to the mount. When <paramref name="raw"/> is <c>false</c>,
        /// this is the bare command (e.g., "GVP") which will be automatically framed with LX200
        /// protocol characters (colon prefix and hash suffix).
        /// </param>
        /// <param name="raw">
        /// When <c>false</c>, the command is wrapped with LX200 framing: a <c>:</c> prefix and <c>#</c> suffix
        /// are added before transmission (e.g., "GVP" becomes ":GVP#").
        /// When <c>true</c>, the command string is sent exactly as provided with no modification.
        /// </param>
        /// <param name="commandtype">
        /// Specifies the expected response type:
        /// <list type="bullet">
        /// <item><description>0 = Blind (fire-and-forget): sends command with no expected response, returns empty string.</description></item>
        /// <item><description>1 = Boolean: expects a single-digit response, returns it as a string.</description></item>
        /// <item><description>2 = String: expects a <c>#</c>-terminated response string.</description></item>
        /// </list>
        /// </param>
        /// <returns>
        /// The mount's response: empty string for blind commands, a single-digit string for boolean commands,
        /// or a <c>#</c>-terminated string for string commands.
        /// </returns>
        /// <exception cref="ASCOM.DriverException">
        /// Thrown when an invalid <paramref name="commandtype"/> is provided, or when retry attempts
        /// are exhausted after a timeout.
        /// </exception>
        /// <remarks>
        /// <para>Thread safety: all calls are serialized via <see cref="LockObject"/> to prevent
        /// concurrent serial port access.</para>
        /// <para>On a COM timeout (HResult code 1026), the method automatically retries via
        /// <see cref="CommanderReTx"/> up to 5 times. After a successful retry, the retransmit
        /// buffer is cleared to maintain command/response synchronization.</para>
        /// </remarks>
        internal static string Commander(string command, bool raw, int commandtype)
        {

            lock (LockObject)
            {
                try
                {
                    CheckConnected("Commander");
                    //CheckParked("Commander");
                }
                catch (Exception ex)
                {
                    LogMessage("Commander", $"Exception: {ex.Message}");
                }

                // Apply LX200 protocol framing if not sending a raw command
                if (!raw) { command = ":" + command + "#"; }
                try
                {
                    // Dispatch based on expected response type
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
                   
                    // Check for COM timeout: HResult low 16 bits == 1026 (0x0402) indicates a serial read timeout.
                    // This commonly occurs when polling slewing status at the end of a goto command.
                    if ((ex is System.Runtime.InteropServices.COMException) && (ex.HResult & 0xFFFF).Equals(1026))
                    {
                        LogMessage("Commander", $"{ex}");
                        LogMessage("Commander", $"isFailure: {(ex.HResult & 0x80000000) != 0}; facility: {(ex.HResult & 0x7FFF0000) >> 16}; code: {ex.HResult & 0xFFFF}");
                        LogMessage("Commander", "Timeout Detected, retransmitting...");            
                        try
                        {
                            int retx = 0;
                            // Retry up to 6 attempts (0..5). Each iteration blocks for the serial read
                            // timeout duration (RECEIVETIMEOUT in SharedResources).
                            while (retx <= 5)
                            {
                                LogMessage("Commander", $"Retry #: {retx + 1}");
                                string result = CommanderReTx(command, commandtype);
                                if (result.Equals("timeout"))
                                {
                                    retx += 1;                            
                                }
                                else
                                {
                                    LogMessage("Commander", $"Retry succeeded for {command} after {retx+1} retries.");
                                    // The mount queues commands and responses 1:1. A timeout causes a mismatch,
                                    // so we must clear the retransmit buffer to re-synchronize the queue.
                                    SharedResources.ClearReTxBuff();
                                    return result;
                                }
                            }
                            LogMessage("Commander", $"Retry failed for {command} after {retx+1} retries.");
                            LogMessage("Commander", $"Trying to clear buffer...");
                            SharedResources.ClearReTxBuff();
                            throw new DriverException($"Retry failed for {command} after {retx+1} retries");

                        }
                        catch (Exception ex1)
                        {

                            LogMessage("Commander Retransmit", $"Error: {ex1.Message}");
                            throw ex1;

                        }
                        
                    }
                    else
                    {
                        throw ex;
                    }

                }
            }
        }

        /// <summary>
        /// Retransmits a command that previously timed out. Called by <see cref="Commander"/> as part
        /// of the retry loop. Unlike Commander, this method does not apply protocol framing (the command
        /// is already framed) and does not acquire <see cref="LockObject"/> (the caller already holds it).
        /// </summary>
        /// <param name="command">The fully-framed command string to retransmit (already includes protocol characters).</param>
        /// <param name="commandtype">
        /// Command type indicating expected response:
        /// 0 = blind (fire-and-forget), 1 = boolean (single digit), 2 = string (#-terminated).
        /// </param>
        /// <returns>
        /// <para>The mount's response on success: empty string for blind commands, the response string for bool/string commands.</para>
        /// <para>Returns the literal string <c>"timeout"</c> if another COM timeout occurs (HResult code 1026),
        /// allowing the caller to continue retrying.</para>
        /// </returns>
        /// <exception cref="ASCOM.DriverException">Thrown for invalid command types.</exception>
        /// <remarks>
        /// The timeout detection uses the same HResult code 1026 check as <see cref="Commander"/>.
        /// Non-timeout exceptions are re-thrown to the caller. The caller is responsible for clearing
        /// the retransmit buffer after a successful retry via <see cref="SharedResources.ClearReTxBuff"/>.
        /// </remarks>
        internal static string CommanderReTx(string command, int commandtype)
        {
            switch (commandtype)
            {
                case 0:
                    try
                    {
                        LogMessage("Commander Retransmit", $"Blind - command {command}");
                        SharedResources.SendMessage(command, commandtype);
                        LogMessage("Commander Retransmit", $"Blind - {command} Completed");
                        return "";
                    }
                    catch (Exception ex)
                    {
                        if ((ex is System.Runtime.InteropServices.COMException) && (ex.HResult & 0xFFFF).Equals(1026))
                        {
                            return "timeout";
                        }
                        else
                        {
                            LogMessage("Commander Retransmit", $"Blind - Error: {ex.Message}; Command: {command}");
                            throw;
                        }
                    }
                case 1:
                    try
                    {
                        LogMessage("Commander Retransmit", $"Bool - command {command}");
                        string retbool = SharedResources.SendMessage(command, commandtype);
                        return retbool;
                    }
                    catch (Exception ex)
                    {
                        if ((ex is System.Runtime.InteropServices.COMException) && (ex.HResult & 0xFFFF).Equals(1026))
                        {
                            return "timeout";
                        }
                        else
                        {
                            LogMessage("Commander Retransmit", $"Blind - Error: {ex.Message}; Command: {command}");
                            throw;
                        }
                    }
                case 2:
                    try
                    {
                        LogMessage("Commander Retransmit", $"String - command {command}");
                        var result = SharedResources.SendMessage(command, commandtype);  //assumes that all return strings are # terminated...is this true?
                                                                                         //tl.LogMessage("CommandString", "utilities.WaitForMilliseconds(TRANSMIT_WAIT_TIME);");
                                                                                         //utilities.WaitForMilliseconds(TRANSMIT_WAIT_TIME); //limit transmit rate
                                                                                         //tl.LogMessage("CommandString", "completed serial port receive...");
                        LogMessage("Commander Retransmit", $"String - {command} Completed: {result}");
                        return result;
                    }
                    catch (Exception ex)
                    {
                        if ((ex is System.Runtime.InteropServices.COMException) && (ex.HResult & 0xFFFF).Equals(1026))  //Indicates a timeout occurred
                        {
                            return "timeout";
                        }
                        else
                        {
                            LogMessage("Commander Retransmit", $"Blind - Error: {ex.Message}; Command: {command}");
                            throw;
                        }
                    }
                default:
                    throw new ASCOM.DriverException("Invalid Command Type: " + commandtype.ToString());
            }
        }

        /// <summary>
        /// [DEPRECATED — Not implemented] ASCOM standard blind command interface.
        /// Would wrap <see cref="Commander"/> with <c>commandtype=0</c> (fire-and-forget, no response expected).
        /// </summary>
        /// <param name="command">The literal command string to be transmitted.</param>
        /// <param name="raw">
        /// If set to <c>true</c> the string is transmitted as-is.
        /// If set to <c>false</c> then LX200 protocol framing (<c>:</c> prefix and <c>#</c> suffix) would be added.
        /// </param>
        /// <exception cref="ASCOM.MethodNotImplementedException">Always thrown; this method is not implemented.</exception>
        /// <remarks>
        /// All command traffic in this driver flows through <see cref="Commander"/> directly rather than
        /// through these ASCOM standard wrappers.
        /// </remarks>
        public static void CommandBlind(string command, bool raw)
        {

            LogMessage("CommandBlind", "Not implemented");
            throw new MethodNotImplementedException("CommandBlind");

        }

        /// <summary>
        /// [DEPRECATED — Not implemented] ASCOM standard boolean command interface.
        /// Would wrap <see cref="Commander"/> with <c>commandtype=1</c> (single-digit boolean response).
        /// </summary>
        /// <param name="command">The literal command string to be transmitted.</param>
        /// <param name="raw">
        /// If set to <c>true</c> the string is transmitted as-is.
        /// If set to <c>false</c> then LX200 protocol framing (<c>:</c> prefix and <c>#</c> suffix) would be added.
        /// </param>
        /// <returns>The interpreted boolean response from the device.</returns>
        /// <exception cref="ASCOM.MethodNotImplementedException">Always thrown; this method is not implemented.</exception>
        /// <remarks>
        /// All command traffic in this driver flows through <see cref="Commander"/> directly rather than
        /// through these ASCOM standard wrappers.
        /// </remarks>
        public static bool CommandBool(string command, bool raw)
        {

            LogMessage("CommandBool", "Not implemented");
            throw new MethodNotImplementedException("CommandBool");

        }

        /// <summary>
        /// [DEPRECATED — Not implemented] ASCOM standard string command interface.
        /// Would wrap <see cref="Commander"/> with <c>commandtype=2</c> (#-terminated string response).
        /// </summary>
        /// <param name="command">The literal command string to be transmitted.</param>
        /// <param name="raw">
        /// If set to <c>true</c> the string is transmitted as-is.
        /// If set to <c>false</c> then LX200 protocol framing (<c>:</c> prefix and <c>#</c> suffix) would be added.
        /// </param>
        /// <returns>The string response received from the device.</returns>
        /// <exception cref="ASCOM.MethodNotImplementedException">Always thrown; this method is not implemented.</exception>
        /// <remarks>
        /// All command traffic in this driver flows through <see cref="Commander"/> directly rather than
        /// through these ASCOM standard wrappers.
        /// </remarks>
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
            astroUtils.Dispose();
            astroUtils = null;
            T.Dispose();

        }

        /// <summary>
        /// Asynchronously connect to the telescope hardware if not already connected.
        /// Uses <see cref="Connecting"/> as the completion flag.
        /// </summary>
        /// <param name="uniqueId">Unique GUID identifying the calling driver instance.</param>
        /// <remarks>
        /// <para>Supports multi-instance connection tracking via the <c>uniqueIds</c> list. If the
        /// <paramref name="uniqueId"/> is already in the list, the request is silently ignored.</para>
        /// <para>The actual connection work is dispatched to a background <see cref="Task"/> that calls
        /// <see cref="SetConnected"/>. The <see cref="Connecting"/> property is set to <c>true</c>
        /// before the task starts and reset to <c>false</c> in the task's <c>finally</c> block,
        /// allowing callers to poll for completion.</para>
        /// <para>If this is the first driver instance to connect, the physical serial link to the
        /// mount is established and first-connect initialization is performed (see <see cref="SetConnected"/>).</para>
        /// </remarks>
        public static void Connect(Guid uniqueId)
        {
            //MessageBox.Show("Wait!");
            LogMessage("Connect", $"Device instance unique ID: {uniqueId}");
            LogMessage("Connect", $"Currently connected driver ids:");
            foreach (Guid id in uniqueIds)
            {
                LogMessage("Connected", $" ID {id} is connected");
            }

            // Check whether this driver instance has already connected
            if (uniqueIds.Contains(uniqueId)) // Instance already connected
            {
                // Ignore the request, the unique ID is already in the list
                LogMessage("Connect", $"Ignoring request to connect because the device is already connected.");
                return;
            }

            // Set the connection in progress flag
            connecting = true;

            // Driver instance not yet connected, so start a task to connect to the device hardware and return while the task runs in the background
            // Discard the returned task value because this a "fire and forget" task
            LogMessage("Connect", $"Starting Connect task...");
            
            _ = Task.Run(() =>
            {
                try
                {
                    // Set the Connected state to true, waiting until it completes
                    LogMessage("ConnectTask", $"Setting connection state to true");
                    SetConnected(uniqueId, true);
                    LogMessage("ConnectTask", $"Connected set true");
                }
                catch (Exception ex)
                {
                    LogMessage("ConnectTask", $"Exception - {ex.Message}\r\n{ex}");
                    throw;
                }
                finally
                {
                    connecting = false;
                    LogMessage("ConnectTask", $"Connecting set false");
                }
            });
            
            LogMessage("Connect", $"Connect task started OK");
            LogMessage("Connect", $"Connected state: {Connected}");
        }

        /// <summary>
        /// Asynchronously disconnect from the telescope hardware.
        /// Uses <see cref="Connecting"/> as the completion flag.
        /// </summary>
        /// <param name="uniqueId">Unique GUID identifying the calling driver instance.</param>
        /// <remarks>
        /// <para>If the <paramref name="uniqueId"/> is not in the connected list, the request is silently ignored
        /// (the instance is already disconnected).</para>
        /// <para>Like <see cref="Connect"/>, the work is dispatched to a background <see cref="Task"/>
        /// that calls <see cref="SetConnected"/> with <c>false</c>. The <see cref="Connecting"/> flag
        /// tracks completion.</para>
        /// <para>The driver instance's unique ID is removed from the <c>uniqueIds</c> list. If this was the
        /// last connected instance, the physical serial link to the mount hardware is closed.</para>
        /// </remarks>
        public static void Disconnect(Guid uniqueId)
        {
            LogMessage("Disconnect", $"Device instance unique ID: {uniqueId}");

            
            // Check whether this driver instance has already disconnected
            if (!uniqueIds.Contains(uniqueId)) // Instance already disconnected
            {
                // Ignore the request, the unique ID is not in the list
                LogMessage("Disconnect", $"Ignoring request to disconnect because the device is already disconnected.");
                return;
            }

            // Set the Disconnect in progress flag
            connecting = true;

            // Start a task to disconnect from the device hardware and return while the task runs in the background
            // Discard the returned task value because this a "fire and forget" task
            LogMessage("Disconnect", $"Starting Disconnect task...");
            _ = Task.Run(() =>
            {
                try
                {
                    // Set the Connected state to false, waiting until it completes
                    LogMessage("DisconnectTask", $"Setting connection state to false");
                    SetConnected(uniqueId, false);
                    LogMessage("DisconnectTask", $"Connected set false");
                }
                catch (Exception ex)
                {
                    LogMessage("DisconnectTask", $"Exception - {ex.Message}\r\n{ex}");
                    throw;
                }
                finally
                {
                    connecting = false;
                    LogMessage("DisconnectTask", $"Connecting set false");
                }
            });
            LogMessage("Disconnect", $"Disconnect task started OK");
        }

        /// <summary>
        /// Completion variable for the asynchronous Connect() and Disconnect()  methods
        /// </summary>
        public static bool Connecting
        {
            get
            {
                return connecting;
            }
        }

        /// <summary>
        /// Synchronously connect to or disconnect from the telescope hardware.
        /// Called by the background tasks in <see cref="Connect"/> and <see cref="Disconnect"/>.
        /// </summary>
        /// <param name="uniqueId">Unique GUID identifying the calling driver instance.</param>
        /// <param name="newState">
        /// <c>true</c> to connect, <c>false</c> to disconnect.
        /// </param>
        /// <remarks>
        /// <para><b>Connection (newState=true):</b> If this is the first instance connecting
        /// (uniqueIds is empty and SharedResources is not connected), performs first-time initialization:</para>
        /// <list type="number">
        /// <item><description>Opens the serial port via <see cref="SharedResources"/>.</description></item>
        /// <item><description>Queries mount firmware version via LX200 <c>:GVN#</c> command and sets
        /// <see cref="DEV_FIRMWARE"/> flag if version >= 355 (enables advanced features).</description></item>
        /// <item><description>Reads site latitude/longitude from the mount.</description></item>
        /// <item><description>Optionally syncs the mount's clock to the computer's UTC time.</description></item>
        /// <item><description>On advanced firmware: configures Align-on-Sync mode and park location settings.</description></item>
        /// </list>
        /// <para>If other instances are already connected, simply increments the connection count.</para>
        /// <para><b>Disconnection (newState=false):</b> Removes the driver ID from the connected list,
        /// saves site coordinates, and decrements the shared connection count. When the last instance
        /// disconnects, the hardware serial link is closed.</para>
        /// </remarks>
        public static void SetConnected(Guid uniqueId, bool newState)
        {
            // Check whether we are connecting or disconnecting
            if (newState) // We are connecting
            {
                // Check whether this driver instance has already connected
                if (uniqueIds.Contains(uniqueId)) // Instance already connected
                {
                    // Ignore the request, the unique ID is already in the list
                    LogMessage("SetConnected", $"Ignoring request to connect because the device is already connected.");
                }
                else // Instance not already connected, so connect it
                {
                    // Check whether this is the first connection to the hardware
                    if (uniqueIds.Count == 0 && !SharedResources.Connected) // This is the first connection to the hardware so initiate the hardware connection (verify with old method)
                    {
                        LogMessage("SetConnected", $"No existing connection found, performing first time connect...");
                        //First time connection
                        try
                        {
                            /*
                            if (AtPark)
                            {
                                LogMessage("SetConnected", "Mount appears parked.  Cycle power and disconnect from all programs to connect");
                                throw new ASCOM.ParkedException("Mount appears parked.  Cycle mount power and disconnect from all programs to connect");
                            }
                            */

                            //Define new serial object.  TTS-160 connects at 9600 baud, 8 data, no parity, 1 stop
                            SharedResources.comPort = profileProperties.ComPort;
                            LogMessage("SetConnected", $"Connecting to {profileProperties.ComPort}");
                            SharedResources.Connected = true;
                            connectedState = true;
                        }
                        catch (Exception ex)
                        {
                            LogMessage("SetConnected", $"Error when connecting: {ex.Message}");
                            connectedState = false;
                            throw;
                        }

                        try
                        {
                            LogMessage("SetConnected", "Success");
                            LogMessage("SetConnected", $"Connected with {Description}");
                            // Query mount product name via LX200 :GVP# command
                            LogMessage("SetConnected", $"Mount Name: {Commander(":GVP#", true, 2).TrimEnd('#')}");

                            // --- Firmware version detection ---
                            // Query firmware version string via LX200 :GVN# command (e.g., "356.0.0")
                            string firmware = Commander(":GVN#", true, 2).TrimEnd('#');
                            int devtest = 0;
                            try
                            {
                                // Parse the first 3 characters as an integer (e.g., "356" -> 356)
                                // to compare against the minimum advanced firmware version threshold
                                devtest = int.Parse(firmware.Substring(0,3));
                            }
                            catch
                            {
                                // Non-numeric firmware string; treat as legacy firmware
                                devtest = 0;
                            }

                            // Firmware versions >= 355 support advanced features (Align-on-Sync, park locations, etc.)
                            if (devtest >= 355)
                            {
                                DEV_FIRMWARE = true;
                                LogMessage("SetConnected", $"Advanced firmware detected: {devtest}.  Enabling advanced features.");
                            }
                            else
                            {
                                DEV_FIRMWARE = false;
                                LogMessage("SetConnected", $"Advanced firmware not detected: {devtest}.  Disabling advanced features.");
                            }
                            LogMessage("SetConnected", $"Mount Firmware Version: {firmware}");
                            LogMessage("SetConnected", $"Mount Firmware Date: {Commander(":GVD#", true, 2)}");
                            LogMessage("SetConnected", "Updating Site Lat and Long");
                            profileProperties.SiteLatitude = SiteLatitudeInit;
                            profileProperties.SiteLongitude = SiteLongitudeInit;
                            LogMessage("SetConnected", $"Mount Lat: {profileProperties.SiteLatitude}");
                            LogMessage("SetConnected", $"Mount Long: {profileProperties.SiteLongitude}");
                            LogMessage("SetConnected", $"Driver Site Location Override: {profileProperties.DriverSiteOverride}");
                            LogMessage("SetConnected", $"Driver Lat: {profileProperties.DriverSiteLatitude}");
                            LogMessage("SetConnected", $"Driver Long: {profileProperties.DriverSiteLongitude}");
                            LogMessage("SetConnected", $"Equatorial Pulse Guide: {profileProperties.PulseGuideEquFrame}");
                            WriteProfile(profileProperties);

                            // --- Time synchronization ---
                            // If enabled in profile settings, sync the mount's internal UTC clock
                            // to the computer's system time. Logs before/after to show drift correction.
                            if (profileProperties.SyncTimeOnConnect)
                            {
                                LogMessage("SetConnected", "Sync Time on Connect - " + profileProperties.SyncTimeOnConnect.ToString());
                                LogMessage("SetConnected", "Pre Sync Mount UTC: " + UTCDate.ToString("MM/dd/yy HH:mm:ss"));
                                LogMessage("SetConnected", "Pre Sync Computer UTC: " + DateTime.UtcNow.ToString("MM/dd/yy HH:mm:ss"));
                                UTCDate = DateTime.UtcNow;
                                LogMessage("SetConnected", "Post Sync Mount UTC: " + UTCDate.ToString("MM/dd/yy HH:mm:ss"));
                                LogMessage("SetConnected", "Post Sync Computer UTC: " + DateTime.UtcNow.ToString("MM/dd/yy HH:mm:ss"));
                            }                    

                            // Clear any previous slew target coordinates from a prior session
                            MiscResources.IsTargetDecSet = false;
                            MiscResources.IsTargetRASet = false;
                            MiscResources.IsTargetSet = false;

                            // --- Advanced firmware features (version >= 355 only) ---
                            if (DEV_FIRMWARE)
                            {
                                // Configure Align-on-Sync if enabled in profile settings.
                                // Sends the desired number of alignment points and verifies the mount echoes it back.
                                if (profileProperties.AlignOnSyncEnabled)
                                {
                                    LogMessage("SetConnected", $"Enabling Align on Sync mode with {profileProperties.AlignOnSyncPoints} alignment points");
                                    int alignpoints = profileProperties.AlignOnSyncPoints;
                                    string cmd = $":**{profileProperties.AlignOnSyncPoints}#";
                                    string resp = Commander(cmd, true, 2);
                                    int respint = int.Parse(resp.TrimEnd('#'));
                                    LogMessage("SetConnected", $"Received number of alignment points: {respint}");
                                    if (!(respint == alignpoints))
                                    {
                                        string msg = "Unexpected response from handpad when setting Align on Sync mode, verify Align On Sync is enabled.";
                                        LogMessage("SetConnected", msg);
                                        throw new DriverException(msg);
                                    }
                                    else
                                    {
                                        MiscResources.AlignOnSyncEnabled = true;
                                        MiscResources.AlignOnSyncPoints = alignpoints;
                                        LogMessage("SetConnected", "Align On Sync mode is active.");
                                    }
                                }

                                // --- Park location initialization ---
                                // If the user updated the park location in the setup dialog, push it to the mount.
                                // :*PS1<az><alt># sets a custom park location; :*PS0# sets park-in-place mode.
                                if (profileProperties.SetParkLoc)
                                {
                                    LogMessage("SetConnected", "Sending updated Park Location to Mount.");
                                    LogMessage("SetConnected", $"Park in Place: {!profileProperties.ParkLoc}");
                                    LogMessage("SetConnected", $"Custom Park Location Altitude: {profileProperties.ParkLocAlt}");
                                    LogMessage("SetConnected", $"Custom Park Location Azimuth: {profileProperties.ParkLocAz}");
                                    if (profileProperties.ParkLoc)
                                    {
                                        // Send custom park coordinates: azimuth (DDD.ddd) + altitude (DD.ddd)
                                        Commander($":*PS1{profileProperties.ParkLocAz.ToString("D3.3")}{profileProperties.ParkLocAlt.ToString("D2.3")}#", true, 0);
                                    }
                                    else
                                    {
                                        // Park-in-place mode: mount parks wherever it currently points
                                        Commander(":*PS0#", true, 0);
                                    }
                                    profileProperties.SetParkLoc = false;
                                }

                                // Read back the mount's current park location settings via :*PG#
                                // Response format: <mode><azimuth 7 chars><altitude 6 chars>
                                LogMessage("SetConnected", "Getting Mount Park Location Settings");
                                string parkstatus = Commander(":*PG#", true, 2);
                                if ((parkstatus[0] - '0') == 0) { profileProperties.ParkLoc = false; }  // '0' = park-in-place
                                else { profileProperties.ParkLoc = true; }  // '1' = custom park location
                                profileProperties.ParkLocAz = Double.Parse(parkstatus.Substring(1, 7));
                                profileProperties.ParkLocAlt = Double.Parse(parkstatus.Substring(8, 6));
                                LogMessage("SetConnected", $"Park in Place: {!profileProperties.ParkLoc}");
                                LogMessage("SetConnected", $"Custom Park Location Altitude: {profileProperties.ParkLocAlt}");
                                LogMessage("SetConnected", $"Custom Park Location Azimuth: {profileProperties.ParkLocAz}");

                            }

                        }
                        catch (Exception ex)
                        {
                            LogMessage("SetConnected", $"Error during initial configuration: {ex.Message}");
                            if (SharedResources.Connected)
                            {
                                LogMessage("SetConnected", $"Hardware remains connected due to legacy connection usage.  Connection Count: {SharedResources.Connections}");
                            }
                        }
                    }
                    else // Other device instances are connected so the hardware is already connected
                    {
                        // Since the hardware is already connected no action is required
                        LogMessage("SetConnected", $"Hardware already connected, incrementing connection count.");
                        SharedResources.Connected = true;
                    }

                    // The hardware either "already was" or "is now" connected, so add the driver unique ID to the connected list
                    uniqueIds.Add(uniqueId);
                    LogMessage("SetConnected", $"Unique id {uniqueId} added to the connection list.");
                }
            }
            else // We are disconnecting
            {
                // Check whether this driver instance has already disconnected
                if (!uniqueIds.Contains(uniqueId)) // Instance not connected so ignore request
                {
                    // Ignore the request, the unique ID is not in the list
                    LogMessage("SetConnected", $"Ignoring request to disconnect because the device is already disconnected.");
                }
                else // Instance currently connected so disconnect it
                {
                    // Remove the driver unique ID to the connected list
                    uniqueIds.Remove(uniqueId);
                    LogMessage("SetConnected", $"Unique id {uniqueId} removed from the connection list.");
                    profileProperties.SiteLatitude = SiteLatitudeInit;
                    profileProperties.SiteLongitude = SiteLongitudeInit;
                    WriteProfile(profileProperties);

                    SharedResources.Connected = false;
                    if (!SharedResources.Connected) //Check to see if any connections remain
                    {
                        connectedState = false;
                        LogMessage("SeConnected", "All drivers disconnected, disconnecting from hardware.");
                    }
                    // Check whether there are now any connected driver instances 
                    if (uniqueIds.Count == 0) // There are no connected driver instances so disconnect from the hardware
                    {
                        LogMessage("SetConnected", $"All driver IDs removed");
                    }
                    else // Other device instances are connected so do not disconnect the hardware
                    {
                        // No action is required
                        LogMessage("SetConnected", $"Hardware already connected.");
                    }
                }
            }

            // Log the current connected state
            LogMessage("SetConnected", $"Currently connected driver ids:");
            foreach (Guid id in uniqueIds)
            {
                LogMessage("SetConnected", $" ID {id} is connected");
            }
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
                LogMessage("Connected Get", $"{IsConnected}");
                return IsConnected;
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
                LogMessage("Description get", $"{DriverDescription} v.{driverVersion}");
                return $"{DriverDescription} v.{driverVersion}";
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
                string driverInfo = $"Driver for TTS-160 v.{driverVersion}";
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
                string driverVersionnot = String.Format(CultureInfo.InvariantCulture, $"{version.Major}.{version.Minor}");
                LogMessage("DriverVersion get", driverVersionnot);
                return driverVersionnot;
            }
        }

        /// <summary>
        /// The interface version number that this device supports. 
        /// </summary>
        public static short InterfaceVersion
        {
            //Interface version 4
            get
            {
                LogMessage("InterfaceVersion Get", "4");
                return Convert.ToInt16("4");
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
                MiscResources.IsSlewingToTarget = false;
                MiscResources.SlewSettleStart = DateTime.MinValue;
                MiscResources.EWMoveAxisSettleStart = DateTime.MinValue;
                MiscResources.NSMoveAxisSettleStart = DateTime.MinValue;
                IsPulseGuiding = false;
                MiscResources.IsPulseGuiding = false;
                MiscResources.MovingPrimary = false;
                MiscResources.MovingSecondary = false;
                //Tracking = MiscResources.TrackSetFollower;
                MiscResources.EWMoveAxisStopFlag = false;
                MiscResources.NSMoveAxisStopFlag = false;

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
                    //String ret = Commander(":GW#", true, 2);
                    //switch (ret[0])
                    //{
                        //case 'A': return DeviceInterface.AlignmentModes.algAltAz;
                        //case 'P': return DeviceInterface.AlignmentModes.algPolar;  //This should be the only response from TTS-160
                        //case 'G': return DeviceInterface.AlignmentModes.algGermanPolar;
                        //default: throw new DriverException("Unknown AlignmentMode Reported");
                        
                    //}
                    return DeviceInterface.AlignmentModes.algAltAz;
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

                    double alt = 0.0;
                    if (DEV_FIRMWARE)
                    {
                        LogMessage("Altitude get", "Advanced Method: Max Precision");
                        var result = Commander(":*GA#", true, 2).TrimEnd('#');
                        LogMessage("Altitude get", $"Retrieved value: {result} radians");
                        alt = double.Parse(result, CultureInfo.InvariantCulture) * (180 / Math.PI); ;//convert rad to deg

                    }
                    else
                    {
                        var result = Commander(":GA#", true, 2);
                        alt = utilities.DMSToDegrees(result);
                    }

                    //:GA# Get telescope altitude
                    //Returns: DDD*MM# or DDD*MM'SS#
                    //The current telescope Altitude depending on the selected precision.

                    LogMessage("Altitude get", $"{alt}");
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
                LogMessage("AtHome get", $"{MiscResources.isAtHome}");
                return MiscResources.isAtHome;
            }
        }

        /// <summary>
        /// True if the telescope has been put into the parked state by the <see cref="Park" /> method. Set False by calling the Unpark() method.
        /// </summary>
        internal static bool AtPark
        {
            get
            {
                MiscResources.IsParked = bool.Parse(Commander(":*Pq#", true, 1));
                LogMessage("AtPark get", $"{MiscResources.IsParked}");
                return MiscResources.IsParked;
            }
            //set
            //{
            //    LogMessage("AtPark set", $"{value}");
            //    MiscResources.IsParked = value;
            //}
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
                if (DEV_FIRMWARE)
                {
                    LogMessage("AxisRates get", "Advanced Firmware Detected");
                    LogMessage("AxisRates get", "AxisRates retrieved will be different.");
                }
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

                    double az = 0.0;
                    if (DEV_FIRMWARE)
                    {
                        LogMessage("Azimuth get", "Advanced Method: Max Precision");
                        var result = Commander(":*GZ#", true, 2).TrimEnd('#');
                        LogMessage("Azimuth get", $"Retrieved value: {result} radians");
                        az = double.Parse(result, CultureInfo.InvariantCulture) * (180 / Math.PI); ;//convert rad to deg

                    }
                    else
                    {
                        var result = Commander(":GZ#", true, 2);
                        az = utilities.DMSToDegrees(result);
                    }

                    //:GZ# Get telescope azimuth
                    //Returns: DDD*MM#T or DDD*MM'SS# verify low precision returns with T at the end!
                    //The current telescope Azimuth depending on the selected precision.

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

                    //Assume Home is 180/0
                    LogMessage("CanFindHome get", $"{true}");
                    return true;
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
                    LogMessage("CanSetDeclinationRate", $"{true}");
                    return true;
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
                    LogMessage("CanSetGuideRates", $"{true}");
                    return true;

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

                    LogMessage("CanSetPark", "Get - " + true.ToString());
                    return true;
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
                    LogMessage("CanSetRightAscensionRate get", $"{true}");
                    return true;
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

                    LogMessage("CanSlewAltAz get", $"{true}");
                    return true;
                }
                catch (Exception ex)
                {
                    LogMessage("CanSlewAltAz get", $"Error: {ex.Message}");
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

                    LogMessage("CanSlewAltAzAsync", $"{true}");
                    return true;
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

                    double declination = 0.0;
                    if (DEV_FIRMWARE)
                    {
                        LogMessage("Declination get", "Advanced Method: Max Precision");
                        var result = Commander(":*GD#", true, 2).TrimEnd('#');
                        LogMessage("Declination get", $"Retrieved value: {result} radians");
                        declination = double.Parse(result, CultureInfo.InvariantCulture) * (180 / Math.PI); //convert rad to deg

                    }
                    else
                    {
                        var result = Commander(":GD#", true, 2);
                        declination = utilities.DMSToDegrees(result);
                    }

                    //:GD# Get telescope Declination
                    //Returns: DDD*MM#T or DDD*MM'SS#
                    //The current telescope Declination depending on the selected precision.    

                    LogMessage("Declination get", utilities.DegreesToDMS(declination, ":", ":", ""));
                    return declination;

                }
                catch (Exception ex)
                {
                    LogMessage("Declination get", $"Error: {ex.Message}");
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

                try
                {
                    CheckConnected("DeclinationRate Get");

                    double declinationrate = 0.0;

                    string cmd = ":*RD#"; //Get the current declination rate
                    LogMessage("DeclinationRate get", $"{cmd}");
                    var result = Commander(cmd, true, 2).TrimEnd('#');
                    LogMessage("Declinationrate get", $"Retrieved value: {result} arc sec/sec");
                    declinationrate = double.Parse(result, CultureInfo.InvariantCulture); //return in arc sec/sec
     
                    return declinationrate;

                }
                catch (Exception ex)
                {
                    LogMessage("DeclinationRate get", $"Error: {ex.Message}");
                    throw;
                }

            }
            set
            {

                try
                {
                    //Declination Rate not implemented by TTS-160
                    //LogMessage("DeclinationRate Set", "Not implemented");
                    //throw new PropertyNotImplementedException("DeclinationRate", true);

                    CheckConnected("DeclinationRate Set");

                    LogMessage("DeclinationRate set", $"{value} arc sec/sec");

                    if( TrackingRate != DriveRates.driveSidereal)
                    {
                        throw new InvalidOperationException("Tracking must be set to Sidereal before setting Declination Rate");
                    }

                    if ( Math.Abs(value) > 99.9999999999 )
                    {
                        throw new InvalidValueException("DeclinationRate", value.ToString(CultureInfo.InvariantCulture), "[-99.9999999999, 99.9999999999]");
                    }
                    string cmd = $":*SD{value.ToString("+00.0000000000;-00.0000000000")}#";
                    LogMessage("DeclinationRate set", $"Sending command: {cmd}");
                    Commander(cmd, true, 0);

                }
                catch (Exception ex)
                {
                    LogMessage("DeclinationRate set", $"Error: {ex.Message}");
                    throw;
                }

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

                EquatorialCoordinateType topocentric = EquatorialCoordinateType.equTopocentric;
                EquatorialCoordinateType J2000 = EquatorialCoordinateType.equJ2000;
                
                try
                {
                    LogMessage("MountEpoch", "Retrieving Mount Epoch setting:");
                    bool result = true;
                    result = bool.Parse(Commander(":*E#", true, 1));
                    if (result)
                    {
                        LogMessage("MountEpoch", $"Retrieved {result}, indicating Topocentric Equatorial.");
                        return topocentric;
                    }
                    else
                    {
                        LogMessage("MountEpoch", $"Retrieved {result}, indicating J2000.");
                        return J2000;
                    }
                }
                catch (Exception ex)
                {
                    LogMessage("MountEpoch", $"Error retrieving mount's Epoch setting: {ex.Message}");
                    throw ex;
                }              

                /*
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
                */
            }
        }

        /// <summary>
        /// Locates the telescope's "home" position (synchronous)
        /// </summary>
        internal static void FindHome()
        {
            try
            {
                CheckConnected("FindHome");
                CheckParked("FindHome");
                CheckSlewing("FindHome");

                LogMessage("FindHome", "Moving to Home");
                if (AtHome)
                {
                    LogMessage("FindHome", "Mount is already at Home");
                    return;
                }

                int haz = 180;
                int halt = -1;
                bool result = true;
                while ( (halt <= 9) && result)
                {
                    halt++;
                    T.SiteLatitude = SiteLatitude;
                    T.SiteLongitude = SiteLongitude;
                    T.SiteElevation = SiteElevation;
                    T.SiteTemperature = 20;
                    T.Refraction = false;
                    T.SetAzimuthElevation(haz, halt);
                    TargetDeclination = T.DECTopocentric;
                    TargetRightAscension = T.RATopocentric;
                    result = bool.Parse(Commander(":MS#", true, 1));
                }
                if (result) { throw new ASCOM.InvalidOperationException("Home position is below the horizon, check mount alignment"); }

                _ = Task.Run(() =>
                {
                    try
                    {
                        while (Slewing)
                        {
                            Thread.Sleep(500); //wait 500 msec and see if still slewing
                        }
                        double alt = Altitude;
                        double az = Azimuth;

                        if (((Math.Abs(Math.Floor(alt+halt))) < 2) && (Math.Abs(180-Math.Floor(az)) < 5))
                        {
                            MiscResources.isAtHome = true;
                            LogMessage("FindHome", $"Arrived at home. Alt: {alt}, Az: {az}");
                        }
                        else
                        {
                            string msg = $"Driver did not end at home, please check mount.  Alt: {alt}, Az: {az}";
                            LogMessage("FindHome", msg);
                            throw new DriverException("FindHome Error:" + msg);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage("FindHome", $"Exception: - {ex.Message}\r\n{ex}");
                        throw;
                    }

                });

            }
            catch (Exception ex)
            {
                LogMessage("FindHome", $"Error: {ex.Message}");
                throw;
            }

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
                try
                {
                    CheckConnected("GuideRateDeclination get");

                    double ret = double.Parse(Commander(":*gRG#", true, 2).TrimEnd('#'));
                    double guiderate = 0;

                    switch (ret)
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

                    LogMessage("GuideRateDeclination get", $"HCGuideRate: {ret} = {guiderate} deg/sec");

                    return guiderate;

                }
                catch (Exception ex)
                {
                    throw ex;
                }

            }
            set
            {

                try
                {
                    CheckConnected("GuideRateDeclination set");

                    value *= 3600; //set value to sec/sec from deg/sec
                    if (value < 0)
                        throw new InvalidValueException($"{value / 3600} is less than 0");
                    int val = 0;
                    if (value < 1.5)
                        val = 0;
                    else if (value < 4.0)
                        val = 1;
                    else if (value < 7.5)
                        val = 2;
                    else if (value < 15.0)
                        val = 3;
                    else val = 4;

                    LogMessage("GuideRateDeclination - set", $"{value} arc sec/sec corresponds to {val}.");
                    Commander($":gRS{value}#", true, 0);

                }
                catch (Exception ex)
                {
                    throw ex;
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
                try
                {
                    CheckConnected("GuideRateRightAscension get");

                    double ret = double.Parse(Commander(":*gRG#", true, 2).TrimEnd('#'));
                    double guiderate = 0;

                    switch (ret)
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

                    LogMessage("GuideRateRightAscension get", $"HCGuideRate: {ret} = {guiderate} deg/sec");

                    return guiderate;

                }
                catch (Exception ex)
                {
                    throw ex;
                }

            }
            set
            {
                try
                {
                    CheckConnected("GuideRateRightAscension set");

                    value *= 3600; //set value to sec/sec from deg/sec
                    if (value < 0)
                        throw new InvalidValueException($"{value / 3600} is less than 0");
                    int val = 0;
                    if (value < 1.5)
                        val = 0;
                    else if (value < 4.0)
                        val = 1;
                    else if (value < 7.5)
                        val = 2;
                    else if (value < 15.0)
                        val = 3;
                    else val = 4;

                    LogMessage("GuideRateRightAscension - set", $"{value} arc sec/sec corresponds to {val}.");
                    Commander($":gRS{value}#", true, 0);

                }
                catch (Exception ex)
                {
                    throw ex;
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

                    /*
                    if (profileProperties.PulseGuideDurationCompliant)
                    {
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
                    else
                    {
                        return MiscResources.IsPulseGuiding;
                    }
                    */
                    
                    
                    if(profileProperties.PulseGuideDurationSynchronous)
                    {
                        return false;
                    }
                    else
                    {
                        if ( MiscResources.PulseGuideStart > DateTime.MinValue )
                        {
                            TimeSpan ts = DateTime.Now.Subtract(MiscResources.PulseGuideStart);
                            if (ts.TotalMilliseconds >= MiscResources.PulseGuideDuration)
                            {
                                MiscResources.PulseGuideStart = DateTime.MinValue;
                                MiscResources.IsPulseGuiding = false;
                                return false;
                            }
                            else
                            {
                                return MiscResources.IsPulseGuiding;
                            }
                            
                        }
                        else
                        {
                            return MiscResources.IsPulseGuiding;
                        }

                    }

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
        /// Convert float to two ints.  Taken from: Taken from: https://stackoverflow.com/questions/5124743/algorithm-for-simplifying-decimal-to-fractions/32903747#32903747
        /// </summary>
        /// <param name="Value">The double variable to be analyzed</param>
        /// <param name="accuracy">Indicate how accurate the answer must be (between 0 and 1) </param>
        internal static Fraction RealToFraction(double value, double accuracy)
        {
            if (accuracy <= 0.0 || accuracy >= 1.0)
            {
                throw new ArgumentOutOfRangeException("accuracy", "Must be > 0 and < 1.");
            }

            int sign = Math.Sign(value);

            if (sign == -1)
            {
                value = Math.Abs(value);
            }

            // Accuracy is the maximum relative error; convert to absolute maxError
            double maxError = sign == 0 ? accuracy : value * accuracy;

            int n = (int)Math.Floor(value);
            value -= n;

            if (value < maxError)
            {
                return new Fraction(sign * n, 1);
            }

            if (1 - maxError < value)
            {
                return new Fraction(sign * (n + 1), 1);
            }

            double z = value;
            int previousDenominator = 0;
            int denominator = 1;
            int numerator;

            do
            {
                z = 1.0 / (z - (int)z);
                int temp = denominator;
                denominator = denominator * (int)z + previousDenominator;
                previousDenominator = temp;
                numerator = Convert.ToInt32(value * denominator);
            }
            while (Math.Abs(value - (double)numerator / denominator) > maxError && z != (int)z);

            return new Fraction((n * denominator + numerator) * sign, denominator);
        }

        /// <summary>
        /// Move the telescope in one axis at the given rate.  
        /// </summary>
        /// <param name="Axis">The physical axis about which movement is desired</param>
        /// <param name="Rate">The rate of motion (deg/sec) about the specified axis</param>
        internal static void MoveAxis(TelescopeAxes Axis, double Rate)
        {

            double TPDH = 13033502.0 / 360.0; //ticks per degree on H axis
            double TPDE = 13146621.0 / 360.0; //ticks per degree on E axis
            double TicksPerPulse = 7.0; //ticks per pulse (high speed)
            double ClockFreq = 57600; // cycle/sec
            double TTP = 1.0;
            
            try
            {

                LogMessage("MoveAxis", $"Axis={Axis} rate={Rate}");
                CheckConnected("MoveAxis");
                CheckParked("MoveAxis");
                //CheckGoto("MoveAxis");  //If we are in a goto, we cannot MoveAxis.
                SlewingInternalUpdate();

                if ( DEV_FIRMWARE )
                {

                    if (!MiscResources.MovingPrimary && !MiscResources.MovingSecondary && Slewing && (Rate != 0))
                    {
                        throw new ASCOM.InvalidOperationException("Error: Non-MoveAxis motion detected, MoveAxis unavailable");
                    }

                    LogMessage("MoveAxis", "Advanced Firmware Detected");
                    LogMessage("MoveAxis", "Using Extended MoveAxis Method");
                    LogMessage("MoveAxis", "Converting Rate to Int");
                    if (Rate < -3.5)
                    {
                        throw new InvalidValueException("Rate must be equal to or greater than -3.5 deg/sec");
                    }
                    else if (Rate > 3.5 )
                    {
                        throw new InvalidValueException("Rate must be equal to or less than 3.5 deg/sec");
                    }
                    
                    double absRate = Math.Abs(Rate);
                    int intabsRate =  Convert.ToInt32(absRate);
                    
                    //double speed = absRate; //deg/sec
                    //speed = 1 / speed;  //deg/sec -> sec/deg
                    //double TTP = (speed - -0.0276390) / 0.0932401;
                    
                    
                    switch( Axis )
                    {
                        case TelescopeAxes.axisPrimary:
                            TTP = (ClockFreq * TicksPerPulse) / (absRate * TPDH);
                            break;
                        case TelescopeAxes.axisSecondary:
                            TTP = (ClockFreq * TicksPerPulse) / (absRate * TPDE);
                            break;
                    }
                    

                    LogMessage("MoveAxis", $"Double: {absRate}; Int: {intabsRate}; TTP: {TTP}");
                    LogMessage("MoveAxis", $"Converting inverse TTP {1/TTP} to integer ratio.");
                    Fraction rateratio = RealToFraction(1 / TTP, .0001);
                    int num = rateratio.N;
                    int den = rateratio.D;

                        if (den < 4999)  //Largest number we can scale up to be closer to 9999
                        {
                            int mult = Convert.ToInt32(Math.Floor(Convert.ToDouble(4999 / den)));  //scale the denominator up as close as possible to 9999
                            den *= mult;
                            num *= mult;
                        }
                        else if (den > 9999)
                        {
                            int mult = Convert.ToInt32(Math.Ceiling(Convert.ToDouble( den/9999)));
                            den = Convert.ToInt32(Math.Round(Convert.ToDouble(den) / Convert.ToDouble(mult)));
                            num = Convert.ToInt32(Math.Round(Convert.ToDouble(num)/Convert.ToDouble(rateratio.D) * Convert.ToDouble(den)));
                        }

                    if (num == 0)  //If we are stopping, just set denominator to 9999
                        den = 9999;

                    LogMessage("MoveAxis", $"Num: {num}; Den: {den}; Result: {Convert.ToDouble(num) / Convert.ToDouble(den)}");
                    string nstr = Math.Abs(num).ToString("D4");
                    string dstr = Math.Abs(den).ToString("D4");
                    switch (Axis)
                    {
                        case TelescopeAxes.axisPrimary:
                            switch (Rate.Compare(0))
                            {
                                case ComparisonResult.Equals:
                                    LogMessage("Extended MoveAxis", "Stopping Primary Axis");
                                    Commander(":Qe#", true, 0);
                                    break;
                                case ComparisonResult.Greater:
                                    var movecmde = ":*Me" + nstr + dstr + "#";
                                    LogMessage("Extended MoveAxis", "Sending Command: " + movecmde);
                                    Commander(movecmde, true, 0);
                                    MiscResources.IsSlewing = true;
                                    MiscResources.MovingPrimary = true;
                                    MiscResources.isAtHome = false;
                                    break;
                                case ComparisonResult.Lower:
                                    var movecmdw = ":*Mw" + nstr + dstr + "#";
                                    LogMessage("Extended MoveAxis", "Sending Command: " + movecmdw);
                                    Commander(movecmdw, true, 0);
                                    MiscResources.IsSlewing = true;
                                    MiscResources.MovingPrimary = true;
                                    MiscResources.isAtHome = false;
                                    break;
                            }
                            break;
                        case TelescopeAxes.axisSecondary:
                            switch (Rate.Compare(0))
                            {
                                case ComparisonResult.Equals:
                                    LogMessage("Extended MoveAxis", "Stopping Secondary Axis");
                                    Commander(":Qn#", true, 0);
                                    break;
                                case ComparisonResult.Greater:
                                    var movecmdn = ":*Mn" + nstr + dstr + "#";
                                    LogMessage("Extended MoveAxis", "Sending Command: " + movecmdn);
                                    Commander(movecmdn, true, 0);
                                    MiscResources.IsSlewing = true;
                                    MiscResources.MovingSecondary = true;
                                    MiscResources.isAtHome = false;
                                    break;
                                case ComparisonResult.Lower:
                                    var movecmds = ":*Ms" + nstr + dstr + "#";
                                    LogMessage("Extended MoveAxis", "Sending Command: " + movecmds);
                                    Commander(movecmds, true, 0);
                                    MiscResources.IsSlewing = true;
                                    MiscResources.MovingSecondary = true;
                                    MiscResources.isAtHome = false;
                                    break;
                            }
                            break;
                        default:
                            throw new InvalidValueException($"Invalid axis selected: {Axis}.");
                    }
                }
                else
                {

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
                            if (MiscResources.EWMoveAxisStopFlag)
                            {
                                iter = 100;
                                while (MiscResources.EWMoveAxisStopFlag)
                                {
                                    Thread.Sleep(MOVEAXIS_WAIT_TIME / iter); //check on flag status for MOVE_AXIS_WAIT_TIME
                                }
                                //If the flag is still true, something is wrong

                                if (MiscResources.EWMoveAxisStopFlag)
                                {
                                    LogMessage("MoveAxis", $"{TelescopeAxes.axisPrimary} is trying to stop and is taking too long.  Unknown mount hardware state.");
                                    throw new DriverException($"{TelescopeAxes.axisPrimary} is trying to stop and is taking too long.  Unknown mount hardware state.");
                                }
                            }
                            switch (Rate.Compare(0))
                            {
                                case ComparisonResult.Equals:
                                    LogMessage("MoveAxis", "Primary Axis Stop Movement");
                                    Commander(":Qe#", true, 0);
                                    //:Qe# Halt eastward Slews
                                    //Returns: Nothing
                                    Commander(":Qw#", true, 0);

                                    //:Qw# Halt westward Slews
                                    //Returns: Nothing                              

                                    //Async implementation: Set flag to indicate stop command is active, set current time for settle calc

                                    MiscResources.EWMoveAxisStopFlag = true;
                                    MiscResources.EWMoveAxisSettleStart = DateTime.Now;

                                    /*
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
                                    MiscResources.isAtHome = false;
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
                                    MiscResources.isAtHome = false;
                                    Slewing = true;
                                    break;
                            }
                            break;

                        case TelescopeAxes.axisSecondary:

                            if (MiscResources.NSMoveAxisStopFlag)
                            {
                                iter = 100;
                                while (MiscResources.NSMoveAxisStopFlag)
                                {
                                    Thread.Sleep(MOVEAXIS_WAIT_TIME / iter); //check on flag status for MOVE_AXIS_WAIT_TIME
                                }
                                //If the flag is still true, something is wrong

                                if (MiscResources.NSMoveAxisStopFlag)
                                {
                                    LogMessage("MoveAxis", $"{TelescopeAxes.axisSecondary} is trying to stop and is taking too long.  Unknown mount hardware state.");
                                    throw new DriverException($"{TelescopeAxes.axisSecondary} is trying to stop and is taking too long.  Unknown mount hardware state.");
                                }
                            }
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

                                    //Async implementation: Set flag to indicate stop command is active, set current time for settle calc

                                    MiscResources.NSMoveAxisStopFlag = true;
                                    MiscResources.NSMoveAxisSettleStart = DateTime.Now;

                                    /*
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
                                    */
                                    /*
                                    tl.LogMessage("MoveAxis", "Secondary Axis Stop Movement");
                                    if (!MiscResources.MovingPrimary) //If both primary and secondary are now stopped, restore tracking to what it was.
                                    {
                                        Slewing = false;  //Should slewing be made false here, or do we need to wait until both primary and secondary are not moving?
                                        Tracking = MiscResources.TrackSetFollower;

                                    }
                                    */
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
                                    MiscResources.isAtHome = false;
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
                                    MiscResources.isAtHome = false;
                                    Slewing = true;
                                    break;
                            }

                            break;
                        default:
                            throw new InvalidValueException("Cannot move this axis.");
                    }
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
                    //AtPark = true;
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
                SlewingInternalUpdate();
            }
            catch (Exception ex)
            {
                LogMessage("PulseGuide", $"Error performing pulse guide: {ex.Message}");
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

                    LogMessage("PulseGuideAwesome", $"GuideRate: {guiderate}");

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

                    if (dur1 > 0)
                    {
                        PulseGuideAwesome(Dir1, dur1);
                    }
                    if (dur2 > 0)
                    {
                        PulseGuideAwesome(Dir2, dur2);
                    }

                    if (profileProperties.PulseGuideDurationSynchronous) { Thread.Sleep(Duration); MiscResources.IsPulseGuiding = false; }
                    else 
                    {
                        MiscResources.PulseGuideDuration = Duration;
                        MiscResources.PulseGuideStart = DateTime.Now;
                    }

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

                //Check to see if GuideComp is enabled, then correct pulse length if required
                int maxcomp = profileProperties.GuideCompMaxDelta; //set maximum allowable compensation time in msec (PHD2 is 1 sec)
                int bufftime = profileProperties.GuideCompBuffer; //set buffer time to decrement from max in msec to prevent tripping PHD2 limit
                double maxalt = 89; //Sufficiently close to 90 to allow exceeding maxcomp while preventing divide by zero
                MiscResources.PulseGuideDuration = Duration;

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
                            MiscResources.PulseGuideDuration = Duration;
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
                        Commander(guidecmde, true, 0);

                        break;
                    case GuideDirections.guideNorth:
                        var guidecmdn = ":Mgs" + Duration.ToString("D4") + "#";  //North and south are reversed...
                        LogMessage("GuideNorth", guidecmdn);
                        Commander(guidecmdn, true, 0);

                        break;
                    case GuideDirections.guideSouth:
                        var guidecmds = ":Mgn" + Duration.ToString("D4") + "#";  //North and south are reversed...
                        LogMessage("GuideSouth", guidecmds);
                        //CommandBlind(guidecmds, true);
                        Commander(guidecmds, true, 0);

                        break;
                    case GuideDirections.guideWest:
                        var guidecmdw = ":Mgw" + Duration.ToString("D4") + "#";
                        LogMessage("GuideWest", guidecmdw);
                        Commander(guidecmdw, true, 0);

                        break;
                }

                MiscResources.PulseGuideStart = DateTime.Now;
                if (profileProperties.PulseGuideDurationSynchronous) { Thread.Sleep(Duration); MiscResources.IsPulseGuiding = false; }

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

                IsPulseGuiding = true;
                LogMessage("PulseGuide", "Guiding with Pulse Guide command");
                switch (Direction)
                {
                    case GuideDirections.guideEast:
                        var guidecmde = ":Mgw" + Duration.ToString("D4") + "#";  //Maybe 180 out in Az...
                        LogMessage("GuideEast", guidecmde);
                        Commander(guidecmde, true, 0);

                        break;
                    case GuideDirections.guideNorth:
                        var guidecmdn = ":Mgs" + Duration.ToString("D4") + "#";  //North and south are switched...
                        LogMessage("GuideNorth", guidecmdn);
                        Commander(guidecmdn, true, 0);

                        break;
                    case GuideDirections.guideSouth:
                        var guidecmds = ":Mgn" + Duration.ToString("D4") + "#";  //North and south are switched...
                        LogMessage("GuideSouth", guidecmds);
                        Commander(guidecmds, true, 0);

                        break;
                    case GuideDirections.guideWest:
                        var guidecmdw = ":Mge" + Duration.ToString("D4") + "#";  //Maybe 180 out in Az
                        LogMessage("GuideWest", guidecmdw);
                        Commander(guidecmdw, true, 0);

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
                    double rightAscension = 0.0;
                    if (DEV_FIRMWARE)
                    {
                        LogMessage("RightAscension Get", "Advanced Method: Max Precision");
                        var result = Commander(":*GR#", true, 2).TrimEnd('#');
                        LogMessage("RightAscension get", $"Retrieved value: {result} radians");
                        rightAscension = double.Parse(result, CultureInfo.InvariantCulture) * (180 / Math.PI) / 15; ;//convert rad to hours       
                        rightAscension = astroUtils.ConditionRA(rightAscension);

                    }
                    else
                    {
                        var result = Commander(":GR#", true, 2);
                        rightAscension = utilities.HMSToHours(result);
                    }
                 
                    //:GR# Get telescope Right Ascension
                    //Returns: HH:MM.T# or HH:MM:SS#
                    //The current telescope Right Ascension depending on the selected precision.

                    tl.LogMessage("RightAscension get", utilities.HoursToHMS(rightAscension, ":", ":"));
                    return rightAscension;
                }
                catch (Exception ex)
                {
                    tl.LogMessage("Right Ascension get", $"Error: {ex.Message}");
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

                try
                {
                    CheckConnected("RightAscensionRate Get");

                    double rarate = 0.0;

                    string cmd = ":*RR#"; //Get the current RA rate
                    LogMessage("RightAscensionRate get", $"{cmd}");
                    var result = Commander(cmd, true, 2).TrimEnd('#');
                    LogMessage("RightAscensionRate get", $"Retrieved value: {result} arc sec/sec");

                    rarate = double.Parse(result, CultureInfo.InvariantCulture) * 0.9972695677; //return in sidereal sec/sec
                    LogMessage("RightAscensionRate get", $"Returned Value: {rarate} sidereal sec/sec");

                    return rarate;

                }
                catch (Exception ex)
                {
                    LogMessage("RightAscensionRate get", $"Error: {ex.Message}");
                    throw;
                }

            }
            set
            {

                try
                {
                    //Declination Rate not implemented by TTS-160
                    //LogMessage("DeclinationRate Set", "Not implemented");
                    //throw new PropertyNotImplementedException("DeclinationRate", true);

                    CheckConnected("RightAsecnsionRate Set");

                    if (TrackingRate != DriveRates.driveSidereal)
                    {
                        throw new InvalidOperationException("Tracking must be set to Sidereal before setting RightAscension Rate");
                    }

                    LogMessage("RightAscensionRate set", $"{value} sidereal sec/sec");

                    double rarate = value * 1.00273790935; //convert to arc sec/sec

                    if (Math.Abs(rarate) > 99.9999999999)
                    {
                        throw new InvalidValueException("DeclinationRate", rarate.ToString(CultureInfo.InvariantCulture), "[-99.9999999999, 99.9999999999]");
                    }
                    string cmd = $":*SR{rarate.ToString("+00.0000000000;-00.0000000000")}#";
                    LogMessage("RightAscensionRate set", $"Sending command: {cmd}");
                    Commander(cmd, true, 0);

                }
                catch (Exception ex)
                {
                    LogMessage("DeclinationRate set", $"Error: {ex.Message}");
                    throw;
                }

            }
        }

        /// <summary>
        /// Sets the telescope's park position to be its current position.
        /// </summary>
        internal static void SetPark()
        {
            
            try
            {
                CheckConnected("SetPark");

                profileProperties.ParkLoc = true;
                profileProperties.ParkLocAlt = (int)Math.Round(Altitude);
                profileProperties.ParkLocAz = (int)Math.Round(Azimuth);

                LogMessage("SetPark", "Sending updated Park Location to Mount.");
                LogMessage("SetPark", $"Park in Place: {!profileProperties.ParkLoc}");
                LogMessage("SetPark", $"Custom Park Location Altitude: {profileProperties.ParkLocAlt}");
                LogMessage("SetPark", $"Custom Park Location Azimuth: {profileProperties.ParkLocAz}");
                Commander($":*PS1{profileProperties.ParkLocAz.ToString("D3")}{profileProperties.ParkLocAlt.ToString("D2")}#", true, 0);

            }
            catch (Exception ex)
            {
                throw ex;
            }


        }

        internal static PierSide CalculateSideOfPier(double rightAscension)
        {
            double hourAngle = astroUtils.ConditionHA(SiderealTime - rightAscension);

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
                LogMessage("SideOfPier Set", "Not implemented");
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
                    SlewingInternalUpdate();
                    var result = Commander(":GS#", true, 2).TrimEnd('#');
                    double siderealTime = utilities.HMSToHours(result);
                    double siteLongitude = SiteLongitude;
                    LogMessage("SiderealTime", "Get GMST - " + siderealTime.ToString());
                    siderealTime += siteLongitude / 360.0 * 24.0;
                    siderealTime = astroUtils.ConditionRA(siderealTime);
                    LogMessage("SiderealTime", "Local Sidereal - " + siderealTime.ToString());
                    return siderealTime;
                }
                catch (Exception ex)
                {
                    LogMessage("SiderealTime", $"Error: {ex.Message}");
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

                        var result = "";
                        if ( DEV_FIRMWARE )
                        {

                            LogMessage("SiteLatitude get", "Advanced Method: Max Precision");
                            result = Commander(":*Gt#", true, 2);
                            LogMessage("SiteLatitude get", $"Returned value: {result}");
        
                        }
                        else
                        {
                            result = Commander(":Gt#", true, 2);
                        }
                        

                        //:Gt# Get Site Latitude
                        //Returns: sDD*MM#

                        double siteLatitude = utilities.DMSToDegrees(result);

                        LogMessage("SiteLatitude get", utilities.DegreesToDMS(siteLatitude, ":", ":", ""));
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

                    var result = "";
                    if (DEV_FIRMWARE)
                    {

                        LogMessage("SiteLatitude get", "Advanced Method: Max Precision");
                        result = Commander(":*Gt#", true, 2);
                        LogMessage("SiteLatitude get", $"Returned value: {result}");

                    }
                    else
                    {
                        result = Commander(":Gt#", true, 2);
                    }

                    //:Gt# Get Site Latitude
                    //Returns: sDD*MM#

                    double siteLatitude = utilities.DMSToDegrees(result);

                    LogMessage("SiteLatitudeInit get", utilities.DegreesToDMS(siteLatitude, ":", ":", ""));
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
                        var result = "";
                        if (DEV_FIRMWARE)
                        {

                            LogMessage("SiteLongitude get", "Advanced Method: Max Precision");
                            result = Commander(":*Gg#", true, 2);
                            LogMessage("SiteLongitude get", $"Returned value: {result}");

                        }
                        else
                        {
                            result = Commander(":Gg#", true, 2);
                        }
                        //:Gg# Get Site Longitude
                        //Returns: sDDD*MM#, east negative

                        //New firmware is now West Negative when entering in handpad, reports as east negative still
                        double siteLongitude = -1 * utilities.DMSToDegrees(result); //correct to West negative
                                                                                    //double siteLongitude = utilities.DMSToDegrees(result);

                        LogMessage("SiteLongitude get", utilities.DegreesToDMS(siteLongitude, ":", ":", ""));
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
                    var result = "";
                    if (DEV_FIRMWARE)
                    {

                        LogMessage("SiteLongitude get", "Advanced Method: Max Precision");
                        result = Commander(":*Gg#", true, 2);
                        LogMessage("SiteLongitude get", $"Returned value: {result}");

                    }
                    else
                    {
                        result = Commander(":Gg#", true, 2);
                    }
                    //:Gg# Get Site Longitude
                    //Returns: sDDD*MM#, east negative

                    //New firmware is now West Negative when entering in handpad, reports as east negative still
                    double siteLongitude = -1 * utilities.DMSToDegrees(result); //correct to West negative
                    //double siteLongitude = utilities.DMSToDegrees(result);
                    LogMessage("SiteLongitudeInit get", utilities.DegreesToDMS(siteLongitude, ":", ":", ""));
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
            //LogMessage("SlewToAltAz", "Not implemented");
            //throw new MethodNotImplementedException("SlewToAltAz");

            try
            {
                CheckConnected("SlewToAltAz");
                CheckParked("SlewToAltAz");
                SlewingInternalUpdate();
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

                Tracking = true;
                MiscResources.TrackSetFollower = false; //turn off tracking after slew!
                //MiscResources.SlewAltAzTrackOverride = true;
                LogMessage("SlewToAltAz", "Calling SlewToCoordinates");
                LogMessage("SlewToAltAz", $"Az: {Azimuth}; Alt: {Altitude}");
                LogMessage("SlewToAltAz", "Derived Ra: " + utilities.HoursToHMS(T.RATopocentric, ":", ":") + "; Derived Dec: " + utilities.DegreesToDMS(T.DECTopocentric, ":", ":"));
                SlewToCoordinates(T.RATopocentric, T.DECTopocentric);
                //MiscResources.SlewAltAzTrackOverride = false;
                LogMessage("SlewToAltAz", "Track override disabled");

                TargetDeclination = curtargDec;
                TargetRightAscension = curtargRA;

            }
            catch (Exception ex)
            {
                tl.LogMessage("SlewToAltAz", $"Error: {ex.Message}");
                throw;
            }

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
            //throw new MethodNotImplementedException("SlewToAltAzAsync");

            try
            {
                CheckConnected("SlewToAltAzAsync");
                CheckParked("SlewToAltAzAsync");
                SlewingInternalUpdate();
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

                //Check for mount Epoch
                double sendRA, sendDec;
                bool epoch = MountEpoch;
                if( epoch )
                {
                    sendRA = T.RATopocentric;
                    sendDec = T.DECTopocentric;
                }
                else
                {
                    sendRA = T.RAJ2000;
                    sendDec = T.DecJ2000;
                }

                //MiscResources.SlewAltAzTrackOverride = true;
                Tracking = true;
                MiscResources.TrackSetFollower = false; //turn off tracking after slew!
                LogMessage("SlewToAltAzAsync", "Calling SlewToCoordinatesAsync");
                LogMessage("SlewToAltAzAsync", $"Az: {Azimuth}; Alt: {Altitude}");
                if( epoch )
                    LogMessage("SlewToAltAzAsync", "Derived Ra: " + utilities.HoursToHMS(sendRA, ":", ":") + "; Derived Dec: " + utilities.DegreesToDMS(sendDec, ":", ":"));
                else
                    LogMessage("SlewToAltAzAsync", "J2000 Derived Ra: " + utilities.HoursToHMS(sendRA, ":", ":") + "; Derived Dec: " + utilities.DegreesToDMS(sendDec, ":", ":"));
                SlewToCoordinatesAsync(sendRA, sendDec);
                MiscResources.SlewAltAzTrackOverride = false;
                //tl.LogMessage("SlewToAltAzAsync", "Track override disabled");

                TargetDeclination = curtargDec;
                TargetRightAscension = curtargRA;
            }
            catch (Exception ex)
            {
                tl.LogMessage("SlewToAltAzAsync", $"Error: {ex.Message}");
                throw;
            }

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
                SlewingInternalUpdate();

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
                SlewingInternalUpdate();

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
                SlewingInternalUpdate();

                if (!Tracking && !MiscResources.SlewAltAzTrackOverride) { throw new ASCOM.InvalidOperationException("Cannot SlewToTarget while not Tracking"); }

                if (MiscResources.IsSlewingToTarget) //Are we currently in a GoTo?
                {
                    throw new InvalidOperationException("Error: GoTo In Progress");
                }

                //Check for mount Epoch
                double J2000RA, J2000Dec;
                double topoRA = TargetRightAscension;
                double topoDec = TargetDeclination;
                bool epoch = MountEpoch;
                if (!epoch)
                {
                    LogMessage("SlewToTarget", "Mount is using J2000 coordinates, updating target values on mount");
                    //Convert AltAz to RaDec Topocentric
                    T.SiteLatitude = SiteLatitude;
                    T.SiteLongitude = SiteLongitude;
                    T.SiteElevation = SiteElevation;
                    T.SiteTemperature = 20;
                    T.Refraction = false;
                    T.SetTopocentric(topoRA, topoDec);

                    J2000RA = T.RAJ2000;
                    J2000Dec = T.DecJ2000;

                    LogMessage("SlewToTarget", "Ra: " + utilities.HoursToHMS(topoRA, ":", ":") + "; Derived Dec: " + utilities.DegreesToDMS(topoDec, ":", ":"));
                    LogMessage("SlewToTarget", "J2000 Derived Ra: " + utilities.HoursToHMS(J2000RA, ":", ":") + "; Derived Dec: " + utilities.DegreesToDMS(J2000Dec, ":", ":"));

                    TargetRightAscension = J2000RA;  //Send new coords to mount
                    TargetDeclination = J2000Dec;

                    MiscResources.Target.RightAscension = topoRA;  //Restore Topocentric values to driver variables (since that is what we work in)
                    MiscResources.Target.Declination = topoDec;

                }

                bool wasTracking = Tracking;

                //double TargRA = TargetRightAscension;
                //double TargDec = TargetDeclination;
                //Assume Target is valid due to setting checks

                bool result = bool.Parse(Commander(":MS#", true, 1));
                if (result) { throw new Exception("Unable to slew:" + result + " Object Below Horizon"); }

                MiscResources.isAtHome = false;
                Slewing = true;
                MiscResources.SlewTarget.RightAscension = MiscResources.Target.RightAscension;
                MiscResources.SlewTarget.Declination = MiscResources.Target.Declination;
                MiscResources.IsSlewingToTarget = true;

                //TTS-160 will indicate slew in progress via Distance Bar command (:D#) returning "|#".  If it returns just "#" => slew complete

                int counter = 0;
                int RateLimit = 200; //Wait time between queries, in msec
                int TimeLimit = 180; //How long to wait for slew to finish before throwing error, in sec
                while (Slewing)
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

                /*
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
                } */
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
                SlewingInternalUpdate();

                if (!Tracking && !MiscResources.SlewAltAzTrackOverride) { throw new ASCOM.InvalidOperationException("Cannot SlewToTargetAsync while not Tracking"); }

                //This should, instead, halt the current goto and start a new one                
                if (MiscResources.IsSlewingToTarget) //Are we currently in a GoTo?
                {
                    throw new ASCOM.InvalidOperationException("Error: GoTo In Progress");
                }

                //Check for mount Epoch
                double J2000RA, J2000Dec;
                double topoRA = TargetRightAscension;
                double topoDec = TargetDeclination;
                bool epoch = MountEpoch;
                if (!epoch)
                {
                    LogMessage("SlewToTarget", "Mount is using J2000 coordinates, updating target values on mount");
                    //Convert AltAz to RaDec Topocentric
                    T.SiteLatitude = SiteLatitude;
                    T.SiteLongitude = SiteLongitude;
                    T.SiteElevation = SiteElevation;
                    T.SiteTemperature = 20;
                    T.Refraction = false;
                    T.SetTopocentric(topoRA, topoDec);

                    J2000RA = T.RAJ2000;
                    J2000Dec = T.DecJ2000;

                    LogMessage("SlewToTargetAsync", "Ra: " + utilities.HoursToHMS(topoRA, ":", ":") + "; Derived Dec: " + utilities.DegreesToDMS(topoDec, ":", ":"));
                    LogMessage("SlewToTargetAsync", "J2000 Derived Ra: " + utilities.HoursToHMS(J2000RA, ":", ":") + "; Derived Dec: " + utilities.DegreesToDMS(J2000Dec, ":", ":"));

                    TargetRightAscension = J2000RA;  //Send new coords to mount
                    TargetDeclination = J2000Dec;

                    MiscResources.Target.RightAscension = topoRA;  //Restore Topocentric values to driver variables (since that is what we work in)
                    MiscResources.Target.Declination = topoDec;

                }

                bool result = bool.Parse(Commander(":MS#", true, 1));
                if (result) { throw new ASCOM.InvalidOperationException("Unable to slew: target below horizon"); }  //Need to review other implementation
                //MiscResources.SlewTarget.RightAscension = TargetRightAscension;
                //MiscResources.SlewTarget.Declination = TargetDeclination;
                MiscResources.SlewTarget.RightAscension = MiscResources.Target.RightAscension;
                MiscResources.SlewTarget.Declination = MiscResources.Target.Declination;
                LogMessage("SlewToTargetAsync", $"SlewTarget Set To: RA: {MiscResources.SlewTarget.RightAscension}, Dec: {MiscResources.SlewTarget.Declination}");
                MiscResources.isAtHome = false;
                Slewing = true;
                MiscResources.IsSlewingToTarget = true;

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
            //'Slewing' query (:D#) _is_ implemented in TTS-160, keep track in driver.
            get
            {
                try
                {

                    CheckConnected("Slewing");
                    LogMessage("Slewing get", "Getting Slew Status");
                    //Catching the end of an async slew event                    

                    /*
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
                                TrackSetFollower(MiscResources.TrackSetFollower);
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
                            TrackSetFollower(MiscResources.TrackSetFollower);
                            return false;

                        }
                        else
                        {
                            LogMessage("Slewing get", "Unknown condition while checking slew status, please verify hardware is operating correctly");
                            throw new DriverException("Unknown condition while checking slew status, please verify hardware is operating correctly");
                        }
                    }*/
                    bool slewstatus = false;
                    if (DEV_FIRMWARE )
                    {
                        LogMessage("Slewing get", "Advanced Firmware Detected");
                        LogMessage("Slewing get", "Using advanced slew detection");
                        slewstatus = Commander(":D#", true, 2).Equals("|#");
                        if (!slewstatus)
                        {
                            if (SlewSettleTime > 0)
                            {
                                if (MiscResources.IsSlewing)
                                {
                                    if (MiscResources.SlewSettleStart == DateTime.MinValue)
                                    {
                                        MiscResources.SlewSettleStart = DateTime.Now;
                                        LogMessage("Slewing Status", $"Slew Complete; Commencing Slew Settling");
                                        return true;
                                    }
                                    else if (MiscResources.SlewSettleStart > DateTime.MinValue)
                                    {
                                        TimeSpan ts = DateTime.Now.Subtract(MiscResources.SlewSettleStart);
                                        if (ts.TotalSeconds >= SlewSettleTime)
                                        {
                                            LogMessage("Slewing Status", $"Slew complete; slew settle complete");
                                            MiscResources.IsSlewing = false;
                                            MiscResources.IsSlewingToTarget = false;  //If I was slewing to a target, I am no longer
                                            MiscResources.MovingPrimary = false;
                                            MiscResources.MovingSecondary = false;
                                            MiscResources.SlewSettleStart = DateTime.MinValue;
                                            TrackSetFollower(MiscResources.TrackSetFollower);
                                            return false;
                                        }
                                        else
                                        {
                                            LogMessage("Slewing Status", $"Slew Complete; Slew Settling in progress");
                                            return true;
                                        }
                                    }
                                }
                                else
                                {
                                    LogMessage("Slewing get", $"No movement detected, not previously slewing.  Returning: {slewstatus}");
                                    MiscResources.IsSlewing = false;
                                    MiscResources.IsSlewingToTarget = false;  //If I was slewing to a target, I am no longer
                                    MiscResources.MovingPrimary = false;
                                    MiscResources.MovingSecondary = false;
                                    return slewstatus;
                                }

                            }
                            else
                            {
                                LogMessage("Slewing get", $"Mount reported no movement.  No Settling Time. Returning: {slewstatus}");
                                MiscResources.IsSlewing = false;
                                MiscResources.IsSlewingToTarget = false;  //If I was slewing to a target, I am no longer
                                MiscResources.MovingPrimary = false;
                                MiscResources.MovingSecondary = false;
                                return slewstatus;
                            }
                        }
                        else
                        {
                            LogMessage("Slewing get", $"Mount reported movement, returning: {slewstatus}");
                            MiscResources.IsSlewing = slewstatus;
                            return slewstatus;
                        }
                        
                    }
                    
                    if (MiscResources.IsSlewing && MiscResources.IsSlewingToTarget)
                    {
                        LogMessage("Slewing get", "GoTo in progress, retrieving status");
                        slewstatus = Commander(":D#", true, 2).Equals("|#");
                        if (!slewstatus)
                        {
                            if (SlewSettleTime > 0)
                            {
                                if (MiscResources.SlewSettleStart == DateTime.MinValue)
                                {
                                    MiscResources.SlewSettleStart = DateTime.Now;
                                    LogMessage("Slewing Status", $"{slewstatus}; Commencing Slew Settling");
                                    return MiscResources.IsSlewing;
                                }
                                else if (MiscResources.SlewSettleStart > DateTime.MinValue)
                                {
                                    TimeSpan ts = DateTime.Now.Subtract(MiscResources.SlewSettleStart);
                                    if (ts.TotalSeconds >= SlewSettleTime)
                                    {
                                        LogMessage("Slewing Status", $"{slewstatus}; slew settle complete");
                                        MiscResources.IsSlewing = false;
                                        MiscResources.IsSlewingToTarget = false;
                                        MiscResources.SlewSettleStart = DateTime.MinValue;

                                        //Check that final location is within 1' of target, else throw an error
                                        double RA = RightAscension;
                                        double Dec = Declination;
                                        double TRA = MiscResources.SlewTarget.RightAscension;
                                        double TDec = MiscResources.SlewTarget.Declination;
                                        double errdist = Math.Sqrt(Math.Pow((RA - TRA)*15, 2) + Math.Pow(Dec - TDec, 2)) / 60; //distance in minutes
                                        LogMessage("Slewing get", $"SlewTarget retrieved as: RA: {TRA}, Dec: {TDec}");
                                        LogMessage("Slewing get", $"Slew error distance: {errdist} arcmin, Tolerance: 1 arcmin");

                                        if (errdist > 1.0) //See if errdistance is outside of tolerance
                                        {
                                            
                                            string targRA = utilities.HoursToHMS(TRA);
                                            string targDec = utilities.DegreesToDMS(TDec);
                                            string actRA = utilities.HoursToHMS(RA);
                                            string actDec = utilities.DegreesToDMS(Dec);
                                            LogMessage("Slewing get", $"Slew result outside tolerance. Target Ra: {targRA}; Actual RA: {actRA}; Target Dec: {targDec}; Actual Dec: {actDec}; Error Distance: {errdist} arcmin");
                                            throw new DriverException($"Slew result outside tolerance. Target Ra: {targRA}; Actual RA: {actRA}; Target Dec: {targDec}; Actual Dec: {actDec}; Error Distance: {errdist} arcmin.  Check to ensure mount is operating correctly");

                                        }
                                        TrackSetFollower(MiscResources.TrackSetFollower);
                                        return false;
                                    }
                                    else
                                    {
                                        LogMessage("Slewing Status", $"{true}; Slew Settling in progress");
                                        return MiscResources.IsSlewing;
                                    }
                                }
                                else
                                {
                                    LogMessage("Slewing get", "Unexpected condition detected.  Correcting issue and returning best status.");
                                    MiscResources.SlewSettleStart = DateTime.MinValue;
                                    return MiscResources.IsSlewing;
                                }
                            }
                            else
                            {
                                MiscResources.IsSlewingToTarget = false;
                                MiscResources.IsSlewing = false;

                                //Check that final location is within 1' of target, else throw an error
                                double RA = RightAscension;
                                double Dec = Declination;
                                double TRA = MiscResources.SlewTarget.RightAscension;
                                double TDec = MiscResources.SlewTarget.Declination;
                                double errdist = Math.Sqrt(Math.Pow((RA - TRA)*15, 2) + Math.Pow(Dec - TDec, 2)) / 60; //distance in minutes
                                LogMessage("Slewing get", $"Slew error distance: {errdist} arcmin, Tolerance: 1 arcmin");

                                if (errdist > 1.0) //See if errdistance is outside of tolerance
                                {

                                    string targRA = utilities.HoursToHMS(TRA);
                                    string targDec = utilities.DegreesToDMS(TDec);
                                    string actRA = utilities.HoursToHMS(RA);
                                    string actDec = utilities.DegreesToDMS(Dec);
                                    LogMessage("Slewing get", $"Slew result outside tolerance. Target Ra: {targRA}; Actual RA: {actRA}; Target Dec: {targDec}; Actual Dec: {actDec}; Error Distance: {errdist} arcmin");
                                    throw new DriverException($"Slew result outside tolerance. Target Ra: {targRA}; Actual RA: {actRA}; Target Dec: {targDec}; Actual Dec: {actDec}; Error Distance: {errdist} arcmin.  Check to ensure mount is operating correctly");

                                }

                                return slewstatus;
                            }
                        }
                        else 
                        { 
                            return slewstatus; 
                        }
                    }             
                    else if (MiscResources.EWMoveAxisStopFlag || MiscResources.NSMoveAxisStopFlag)  //If MoveAxis recently received a stop command...
                    {
                        if (Tracking)  //If tracking is on, all movement is stopped, we are good!  Reset everything, return false
                        {
                            LogMessage("Slewing get", "All MoveAxis motion is stopped, resetting flags.");
                            MiscResources.EWMoveAxisStopFlag = false;
                            MiscResources.NSMoveAxisStopFlag = false;
                            MiscResources.EWMoveAxisSettleStart = DateTime.MinValue;
                            MiscResources.NSMoveAxisSettleStart = DateTime.MinValue;
                            MiscResources.MovingPrimary = false;
                            MiscResources.MovingSecondary = false;
                            MiscResources.IsSlewing = false;
                            TrackSetFollower(MiscResources.TrackSetFollower);  //Don't want Tracking set to fail on Slewing check!
                            LogMessage("Slewing get", $"Slewing is: {MiscResources.IsSlewing}");
                            return false;

                        }
                        //Iterate through checking timing on both axes

                        if (!Tracking && MiscResources.EWMoveAxisStopFlag)
                        {
                            //Check to see if the settle time has timed out
                            TimeSpan ts = DateTime.Now.Subtract(MiscResources.EWMoveAxisSettleStart);
                            if (ts.TotalSeconds >= MOVEAXIS_WAIT_TIME / 1000)
                            {
                                LogMessage("Slewing get", "Primary axis MoveAxis motion assumed stop, resetting primary axis flags.");
                                MiscResources.EWMoveAxisStopFlag = false;
                                MiscResources.EWMoveAxisSettleStart = DateTime.MinValue;
                                MiscResources.MovingPrimary = false;

                            }
                            //If not, do nothing
                        }

                        if (!Tracking && MiscResources.NSMoveAxisStopFlag)
                        {
                            //Check to see if the settle time has timed out
                            TimeSpan ts = DateTime.Now.Subtract(MiscResources.NSMoveAxisSettleStart);
                            if (ts.TotalSeconds >= MOVEAXIS_WAIT_TIME / 1000)
                            {
                                LogMessage("Slewing get", "Secondary axis MoveAxis motion assumed stop, resetting secondary axis flags.");
                                MiscResources.NSMoveAxisStopFlag = false;
                                MiscResources.NSMoveAxisSettleStart = DateTime.MinValue;
                                MiscResources.MovingSecondary = false;

                            }
                            //If not, do nothing
                        }
                        //Check to see if all motion is stopped, if so, reset flags, set IsSlewing to false and return.  If not do nothing and just return IsSlewing (which should be true...)

                        if (!MiscResources.MovingPrimary && !MiscResources.MovingSecondary)
                        {
                            LogMessage("Slewing get", "All MoveAxis motion is stopped, resetting flags.");
                            MiscResources.EWMoveAxisStopFlag = false;
                            MiscResources.NSMoveAxisStopFlag = false;
                            MiscResources.EWMoveAxisSettleStart = DateTime.MinValue;
                            MiscResources.NSMoveAxisSettleStart = DateTime.MinValue;
                            MiscResources.IsSlewing = false;
                            TrackSetFollower(MiscResources.TrackSetFollower);  //Don't want Tracking set to fail on Slewing check!
                        }

                        LogMessage("Slewing get", $"Slewing is: {MiscResources.IsSlewing}");
                        return MiscResources.IsSlewing;

                    }
                    else
                    {
                        LogMessage("Slewing get", $"Slewing is: {MiscResources.IsSlewing}");
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
                SlewingInternalUpdate();

                //if (Tracking) { throw new ASCOM.InvalidOperationException("Cannot SyncToAltAz while Tracking"); }

                if ((TAzimuth < 0) || (TAzimuth > 360)) { throw new ASCOM.InvalidValueException($"Invalid Azimuth ${TAzimuth}"); }
                if ((TAltitude < 0) || (TAltitude > 90)) { throw new ASCOM.InvalidValueException($"Invalid Altitude ${TAltitude}"); }

                double presyncAlt = Altitude;
                double presyncAz = Azimuth;

                T.SiteLatitude = SiteLatitude;
                T.SiteLongitude = SiteLongitude;
                T.SiteElevation = SiteElevation;
                T.SiteTemperature = 20;
                T.Refraction = false;
                LogMessage("SyncToAltAz", "Calling T.SetAzimuthElevation Method:");
                T.SetAzimuthElevation(TAzimuth, TAltitude);

                SyncToCoordinates(T.RATopocentric, T.DECTopocentric);
                LogMessage("SyncToCoordinates", $"Sleeping for {SYNC_WAIT_TIME} ms for sync to take...");
                Thread.Sleep(SYNC_WAIT_TIME);

                double postsyncAlt = Altitude;
                double postsyncAz = Azimuth;

                LogMessage("SyncToAltAz", "Complete");
                LogMessage("SyncToAltAz", $"PreSync: Alt: " + utilities.DegreesToDMS(presyncAlt, ":", ":", "") + "; Az: " + utilities.DegreesToDMS(presyncAz, ":", ":", ""));
                LogMessage("SyncToAltAz", $"Target: Alt " + utilities.DegreesToDMS(TAltitude, ":", ":", "") + "; Az: " + utilities.DegreesToDMS(TAzimuth, ":", ":", ""));
                LogMessage("SyncToAltAz", $"PostSync: Alt: " + utilities.DegreesToDMS(postsyncAlt, ":", ":", "") + "; Az: " + utilities.DegreesToDMS(postsyncAz, ":", ":", ""));

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
                SlewingInternalUpdate();

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

                if (DEV_FIRMWARE)
                {

                    //Check for mount Epoch
                    double J2000RA, J2000Dec;
                    double topoRA = TargetRightAscension;
                    double topoDec = TargetDeclination;
                    bool epoch = MountEpoch;
                    if (!epoch)
                    {
                        LogMessage("SyncToTarget", "Mount is using J2000 coordinates, updating target values on mount");
                        //Convert AltAz to RaDec Topocentric
                        T.SiteLatitude = SiteLatitude;
                        T.SiteLongitude = SiteLongitude;
                        T.SiteElevation = SiteElevation;
                        T.SiteTemperature = 20;
                        T.Refraction = false;
                        T.SetTopocentric(topoRA, topoDec);

                        J2000RA = T.RAJ2000;
                        J2000Dec = T.DecJ2000;

                        LogMessage("SyncToTarget", "Ra: " + utilities.HoursToHMS(topoRA, ":", ":") + "; Derived Dec: " + utilities.DegreesToDMS(topoDec, ":", ":"));
                        LogMessage("SyncToTarget", "J2000 Derived Ra: " + utilities.HoursToHMS(J2000RA, ":", ":") + "; Derived Dec: " + utilities.DegreesToDMS(J2000Dec, ":", ":"));

                        TargetRightAscension = J2000RA;  //Send new coords to mount
                        TargetDeclination = J2000Dec;

                        MiscResources.Target.RightAscension = topoRA;  //Restore Topocentric values to driver variables (since that is what we work in)
                        MiscResources.Target.Declination = topoDec;

                    }

                    if (MiscResources.AlignOnSyncEnabled)
                    {
                        LogMessage("SyncToCoordinates", "Advanced Firmware Detected");
                        LogMessage("SyncToCoordinates", $"Align on Sync is: {MiscResources.AlignOnSyncEnabled}");
                        LogMessage("SyncToCoordinates", $"Using extended Sync method");

                        double presyncRA = RightAscension;
                        double presyncDec = Declination;

                        string ret = Commander(":*CM#", true, 2);
                        LogMessage("SyncToCoordinates", "Trying to Parse this string: " + ret);
                        int retpoints = int.Parse(ret.TrimEnd('#'));
                        LogMessage("SyncToCoordinates", $"Parsed as: {retpoints}");
                        retpoints--;

                        if (retpoints > 0)
                        {
                            LogMessage("SyncToCoordinates", $"Complete, {retpoints} points remain.");
                            MiscResources.AlignOnSyncPoints = retpoints;
                        }
                        else if (retpoints == 0)
                        {
                            LogMessage("SyncToCoordinates", $"Complete, {retpoints} points remain.");
                            LogMessage("SyncToCoordinates", $"Disabling Align on Sync mode.");
                            MiscResources.AlignOnSyncEnabled = false;
                            MiscResources.AlignOnSyncPoints = retpoints;
                        }
                        else
                        {
                            LogMessage("SyncToCoordinates", $"Align on Sync failed, disabling Align on Sync mode.");
                            MiscResources.AlignOnSyncEnabled = false;
                            MiscResources.AlignOnSyncPoints = 0;
                        }

                        //var ret = Commander(":*CM#", true, 2);
                        LogMessage("SyncToCoordinates", $"Sleeping for {SYNC_WAIT_TIME} ms for sync to take...");
                        Thread.Sleep(SYNC_WAIT_TIME);
                        double postsyncRA = RightAscension;
                        double postsyncDec = Declination;
                        double targRA = TargetRightAscension;
                        double targDec = TargetDeclination;
                        LogMessage("SyncToCoordinates", $"PreSync: Ra: " + utilities.HoursToHMS(presyncRA, ":", ":", "") + "; Dec: " + utilities.DegreesToDMS(presyncDec, ":", ":", ""));
                        LogMessage("SyncToTarget", $"Assumed Target: Ra: " + utilities.HoursToHMS(targRA, ":", ":", "") + "; Dec: " + utilities.DegreesToDMS(targDec, ":", ":", ""));
                        LogMessage("SyncToCoordinates", $"PostSync: Ra: " + utilities.HoursToHMS(postsyncRA, ":", ":", "") + "; Dec: " + utilities.DegreesToDMS(postsyncDec, ":", ":", ""));

                    }
                    else
                    {

                        double presyncRA = RightAscension;
                        double presyncDec = Declination;
                        LogMessage("SyncToCoordinates", "Advanced Firmware detected.");
                        LogMessage("SyncToCoordinates", $"Align on Sync is: {MiscResources.AlignOnSyncEnabled}");
                        LogMessage("SyncToCoordinates", $"Using old Sync method");               

                        var ret = Commander(":CM#", true, 2);
                        LogMessage("SyncToCoordinates", $"Sleeping for {SYNC_WAIT_TIME} ms for sync to take...");
                        Thread.Sleep(SYNC_WAIT_TIME);
                        double postsyncRA = RightAscension;
                        double postsyncDec = Declination;
                        LogMessage("SyncToCoordinates", $"PreSync: Ra: " + utilities.HoursToHMS(presyncRA, ":", ":", "") + "; Dec: " + utilities.DegreesToDMS(presyncDec, ":", ":", ""));
                        LogMessage("SyncToCoordinates", $"Target: Ra: " + utilities.HoursToHMS(TRightAscension, ":", ":", "") + "; Dec: " + utilities.DegreesToDMS(TDeclination, ":", ":", ""));
                        LogMessage("SyncToCoordinates", $"PostSync: Ra: " + utilities.HoursToHMS(postsyncRA, ":", ":", "") + "; Dec: " + utilities.DegreesToDMS(postsyncDec, ":", ":", ""));

                    }
                }
                else
                {

                    double presyncRA = RightAscension;
                    double presyncDec = Declination;
                    var ret = Commander(":CM#", true, 2);
                    LogMessage("SyncToCoordinates", "Complete: " + ret);
                    LogMessage("SyncToCoordinates", $"Sleeping for {SYNC_WAIT_TIME} ms for sync to take...");
                    Thread.Sleep(SYNC_WAIT_TIME);
                    double postsyncRA = RightAscension;
                    double postsyncDec = Declination;
                    LogMessage("SyncToCoordinates", $"PreSync: Ra: " + utilities.HoursToHMS(presyncRA, ":", ":", "") + "; Dec: " + utilities.DegreesToDMS(presyncDec, ":", ":", ""));
                    LogMessage("SyncToCoordinates", $"Target: Ra: " + utilities.HoursToHMS(TRightAscension, ":", ":", "") + "; Dec: " + utilities.DegreesToDMS(TDeclination, ":", ":", ""));
                    LogMessage("SyncToCoordinates", $"PostSync: Ra: " + utilities.HoursToHMS(postsyncRA, ":", ":", "") + "; Dec: " + utilities.DegreesToDMS(postsyncDec, ":", ":", ""));

                }

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
                SlewingInternalUpdate();   

                if (DEV_FIRMWARE)
                {

                    //Check for mount Epoch
                    double J2000RA, J2000Dec;
                    double topoRA = TargetRightAscension;
                    double topoDec = TargetDeclination;
                    bool epoch = MountEpoch;
                    if (!epoch)
                    {
                        LogMessage("SyncToTarget", "Mount is using J2000 coordinates, updating target values on mount");
                        //Convert AltAz to RaDec Topocentric
                        T.SiteLatitude = SiteLatitude;
                        T.SiteLongitude = SiteLongitude;
                        T.SiteElevation = SiteElevation;
                        T.SiteTemperature = 20;
                        T.Refraction = false;
                        T.SetTopocentric(topoRA, topoDec);

                        J2000RA = T.RAJ2000;
                        J2000Dec = T.DecJ2000;

                        LogMessage("SyncToTarget", "Ra: " + utilities.HoursToHMS(topoRA, ":", ":") + "; Derived Dec: " + utilities.DegreesToDMS(topoDec, ":", ":"));
                        LogMessage("SyncToTarget", "J2000 Derived Ra: " + utilities.HoursToHMS(J2000RA, ":", ":") + "; Derived Dec: " + utilities.DegreesToDMS(J2000Dec, ":", ":"));

                        TargetRightAscension = J2000RA;  //Send new coords to mount
                        TargetDeclination = J2000Dec;

                        MiscResources.Target.RightAscension = topoRA;  //Restore Topocentric values to driver variables (since that is what we work in)
                        MiscResources.Target.Declination = topoDec;

                    }

                    if (MiscResources.AlignOnSyncEnabled)
                    {
                        double presyncRA = RightAscension;
                        double presyncDec = Declination;
                        var ret = Commander(":*CM#", true, 2);
                        int retpoints = int.Parse(ret.TrimEnd('#'));
                        retpoints--;
                        if (retpoints > 0)
                        {
                            LogMessage("SyncToCoordinates", $"Complete, {retpoints} points remain.");
                            MiscResources.AlignOnSyncPoints = retpoints;
                        }
                        else if (retpoints == 0)
                        {
                            LogMessage("SyncToCoordinates", $"Complete, {retpoints} points remain.");
                            LogMessage("SyncToCoordinates", $"Disabling Align on Sync mode.");
                            MiscResources.AlignOnSyncEnabled = false;
                            MiscResources.AlignOnSyncPoints = retpoints;
                        }
                        else
                        {
                            LogMessage("SyncToCoordinates", $"Align on Sync failed, disabling Align on Sync mode.");
                            MiscResources.AlignOnSyncEnabled = false;
                            MiscResources.AlignOnSyncPoints = 0;
                        }
                        LogMessage("SyncToCoordinates", $"Sleeping for {SYNC_WAIT_TIME} ms for sync to take...");
                        Thread.Sleep(SYNC_WAIT_TIME);
                        double postsyncRA = RightAscension;
                        double postsyncDec = Declination;
                        double targRA = TargetRightAscension;
                        double targDec = TargetDeclination;
                        LogMessage("SyncToCoordinates", $"PreSync: Ra: " + utilities.HoursToHMS(presyncRA, ":", ":", "") + "; Dec: " + utilities.DegreesToDMS(presyncDec, ":", ":", ""));
                        LogMessage("SyncToTarget", $"Assumed Target: Ra: " + utilities.HoursToHMS(targRA, ":", ":", "") + "; Dec: " + utilities.DegreesToDMS(targDec, ":", ":", ""));
                        LogMessage("SyncToCoordinates", $"PostSync: Ra: " + utilities.HoursToHMS(postsyncRA, ":", ":", "") + "; Dec: " + utilities.DegreesToDMS(postsyncDec, ":", ":", ""));
                    }
                    else
                    {
                        double presyncRA = RightAscension;
                        double presyncDec = Declination;
                        var ret = Commander(":CM#", true, 2);  //For some reason TTS-160 returns a message and not catching it causes
                                                               //further commands to act funny (results are 1 order off despite the
                                                               //buffer clears)
                        LogMessage("SyncToTarget", "Complete: " + ret);
                        LogMessage("SyncToCoordinates", $"Sleeping for {SYNC_WAIT_TIME} ms for sync to take...");
                        Thread.Sleep(SYNC_WAIT_TIME);
                        double postsyncRA = RightAscension;
                        double postsyncDec = Declination;
                        double targRA = TargetRightAscension;
                        double targDec = TargetDeclination;
                        LogMessage("SyncTotarget", $"PreSync: Ra: " + utilities.HoursToHMS(presyncRA, ":", ":", "") + "; Dec: " + utilities.DegreesToDMS(presyncDec, ":", ":", ""));
                        LogMessage("SyncToTarget", $"Assumed Target: Ra: " + utilities.HoursToHMS(targRA, ":", ":", "") + "; Dec: " + utilities.DegreesToDMS(targDec, ":", ":", ""));
                        LogMessage("SyncToTarget", $"PostSync: Ra: " + utilities.HoursToHMS(postsyncRA, ":", ":", "") + "; Dec: " + utilities.DegreesToDMS(postsyncDec, ":", ":", ""));
                    }
                }
                else
                {
                    double presyncRA = RightAscension;
                    double presyncDec = Declination;
                    var ret = Commander(":CM#", true, 2);  //For some reason TTS-160 returns a message and not catching it causes
                                                           //further commands to act funny (results are 1 order off despite the
                                                           //buffer clears)
                    LogMessage("SyncToTarget", "Complete: " + ret);
                    LogMessage("SyncToCoordinates", $"Sleeping for {SYNC_WAIT_TIME} ms for sync to take...");
                    Thread.Sleep(SYNC_WAIT_TIME);
                    double postsyncRA = RightAscension;
                    double postsyncDec = Declination;
                    double targRA = TargetRightAscension;
                    double targDec = TargetDeclination;
                    LogMessage("SyncTotarget", $"PreSync: Ra: " + utilities.HoursToHMS(presyncRA, ":", ":", "") + "; Dec: " + utilities.DegreesToDMS(presyncDec, ":", ":", ""));
                    LogMessage("SyncToTarget", $"Assumed Target: Ra: " + utilities.HoursToHMS(targRA, ":", ":", "") + "; Dec: " + utilities.DegreesToDMS(targDec, ":", ":", ""));
                    LogMessage("SyncToTarget", $"PostSync: Ra: " + utilities.HoursToHMS(postsyncRA, ":", ":", "") + "; Dec: " + utilities.DegreesToDMS(postsyncDec, ":", ":", ""));
                }

            }
            catch (Exception ex)
            {
                LogMessage("SyncToTarget", $"Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Query the mount for the current Epoch setting. true = JNow, false = J2000
        /// </summary>

        internal static bool MountEpoch
        {
            get
            {
                try
                {
                    LogMessage("MountEpoch", "Retrieving Mount Epoch setting:");
                    bool result = true;
                    result = bool.Parse(Commander(":*E#", true, 1));
                    if (result)
                        LogMessage("MountEpoch", $"Retrieved {result}, indicating Topocentric Equatorial.");
                    else
                        LogMessage("MountEpoch", $"Retrieved {result}, indicating J2000.");
                    return result;
                }
                catch (Exception ex)
                {
                    LogMessage("MountEpoch", $"Error retrieving mount's Epoch setting: {ex.Message}");
                    throw ex;
                }

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
                        LogMessage("TargetDeclination get", $"{MiscResources.Target.Declination}");
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
                        LogMessage("TargetDeclination Set", $"Target Dec Raw:{value}");
                        var targDec = utilities.DegreesToDMS(value, "*", ":");
                        LogMessage("TargetDeclination Set", $"Target Dec String: {targDec}");
                        bool result = false;
                        if (value >= 0)
                        {
                            result = bool.Parse(Commander($":Sd+{targDec}#", true, 1));
                        }
                        else
                        {
                            result = bool.Parse(Commander($":Sd{targDec}#", true, 1));  //negative numbers already have a preceeding (-) sign
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
                        LogMessage("TargetRightAscension get", $"{MiscResources.Target.RightAscension}");
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

                    if (Slewing)
                    {
                        //Rather than throwing an error, this should queue the command to execute once slewing is complete                        
                        LogMessage("Tracking set", "Cannot change tracking state while slewing.");
                        throw new InvalidOperationException("Cannot change tracking state while slewing.");
                    }
                    else
                    {
                        LogMessage("Tracking set", $"Set Tracking Enabled to: {value}");
                        if (value) { Commander(":T1#", true, 0); }
                        else if (!value) { Commander(":T0#", true, 0); }
                        else { throw new ASCOM.InvalidValueException($"Expected True or False, received: {value}"); }
                        MiscResources.TrackSetFollower = value;
                    }

                }
                catch (Exception ex)
                {
                    LogMessage("Tracking set", $"Error: {ex.Message}");
                    throw;
                }

            }
        }

        internal static void TrackSetFollower(bool TrackSetFollower)
        {
            try
            {
                LogMessage("TrackSetFollower", $"Set Tracking Enabled to: {TrackSetFollower}");
                if (TrackSetFollower) { Commander(":T1#", true, 0); }
                else if (!TrackSetFollower) { Commander(":T0#", true, 0); }
                else { throw new ASCOM.InvalidValueException($"Expected True or False, received: {TrackSetFollower}"); }
            }
            catch (Exception ex)
            {
                LogMessage("TrackSetFollower", $"ExecutingTrackSetFollower with {TrackSetFollower}, failed due to {ex.Message}");
                throw;
            }

        }

        internal static void SlewingInternalUpdate()
        {
            try
            {
                if ((MiscResources.SlewSettleStart > DateTime.MinValue) || (MiscResources.EWMoveAxisSettleStart > DateTime.MinValue) || (MiscResources.NSMoveAxisSettleStart > DateTime.MinValue))
                {
                    bool dump = Slewing;
                }
                else if (MiscResources.EWMoveAxisStopFlag || MiscResources.NSMoveAxisStopFlag)
                {
                    bool dump = Slewing;
                }
            }
            catch(Exception ex)
            {
                LogMessage("SlewingInternalUpdate", $"Exception detected during internal update: {ex.Message}");
                throw;
            }
            return;
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

                    string ret = Commander(":*TRG#", true, 2);
                    int retint = int.Parse(ret.TrimEnd('#'));
                    DriveRates curr = DriveRates.driveSidereal;

                    switch( retint )
                    {
                        case 0:
                            curr =  DriveRates.driveSidereal;
                            break;
                        case 1:
                            curr =  DriveRates.driveLunar;
                            break;
                        case 2:
                            curr =  DriveRates.driveSolar;
                            break;
                    }

                    LogMessage("TrackingRate get - ", $"Received: {ret}, corresponding to {curr}, {curr.GetType()}.");

                    return curr;

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
                    string res = Commander(":SC" + newdate + "#", true, 2);
                    bool resBool = char.GetNumericValue(res[0]) == 1;
                    if (!resBool) { throw new ASCOM.InvalidValueException("UTC Date Set Invalid Date: " + newdate); }

                    string newtime = localdatetime.ToString("HH:mm:ss");
                    resBool = bool.Parse(Commander(":SL" + newtime + "#", true, 1));
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
        /// Use this function to throw an exception if we are in a goto
        /// </summary>
        /// <param name="message"></param>
        private static void CheckGoto(string message)
        {
            
            if (Slewing)
            {
                throw new ASCOM.InvalidOperationException("Unable to " + message + " while in a Goto");
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

                    profileProperties.TraceLogger = Convert.ToBoolean(driverProfile.GetValue(DriverProgId, traceStateProfileName, string.Empty, traceStateDefault));
                    profileProperties.ComPort = driverProfile.GetValue(DriverProgId, comPortProfileName, string.Empty, comPortDefault);
                    profileProperties.SiteElevation = Double.Parse(driverProfile.GetValue(DriverProgId, siteElevationProfileName, string.Empty, siteElevationDefault));
                    profileProperties.SlewSettleTime = Int16.Parse(driverProfile.GetValue(DriverProgId, SlewSettleTimeName, string.Empty, SlewSettleTimeDefault));
                    profileProperties.SiteLatitude = Double.Parse(driverProfile.GetValue(DriverProgId, SiteLatitudeName, string.Empty, SiteLatitudeDefault));
                    profileProperties.SiteLongitude = Double.Parse(driverProfile.GetValue(DriverProgId, SiteLongitudeName, string.Empty, SiteLongitudeDefault));
                    profileProperties.SyncTimeOnConnect = Convert.ToBoolean(driverProfile.GetValue(DriverProgId, SyncTimeOnConnectName, string.Empty, SyncTimeOnConnectDefault));
                    profileProperties.GuideComp = Int32.Parse(driverProfile.GetValue(DriverProgId, GuideCompName, string.Empty, GuideCompDefault));
                    profileProperties.GuideCompMaxDelta = Int32.Parse(driverProfile.GetValue(DriverProgId, GuideCompMaxDeltaName, string.Empty, GuideCompMaxDeltaDefault));
                    profileProperties.GuideCompBuffer = Int32.Parse(driverProfile.GetValue(DriverProgId, GuideCompBufferName, string.Empty, GuideCompBufferDefault));
                    profileProperties.PulseGuideEquFrame = Convert.ToBoolean(driverProfile.GetValue(DriverProgId, PulseGuideEquFrameName, string.Empty, PulseGuideEquFrameDefault));
                    profileProperties.DriverSiteOverride = Convert.ToBoolean(driverProfile.GetValue(DriverProgId, DriverSiteOverrideName, string.Empty, DriverSiteOverrideDefault));
                    profileProperties.DriverSiteLatitude = Double.Parse(driverProfile.GetValue(DriverProgId, DriverSiteLatitudeName, string.Empty, DriverSiteLatitudeDefault));
                    profileProperties.DriverSiteLongitude = Double.Parse(driverProfile.GetValue(DriverProgId, DriverSiteLongitudeName, string.Empty, DriverSiteLongitudeDefault));
                    profileProperties.PulseGuideDurationSynchronous = Convert.ToBoolean(driverProfile.GetValue(DriverProgId, PulseGuideDurationSynchronousName, string.Empty, PulseGuideDurationSynchronousDefault));
                    profileProperties.AlignOnSyncEnabled = Convert.ToBoolean(driverProfile.GetValue(DriverProgId, AlignOnSyncEnabledName, string.Empty, AlignOnSyncEnabledDefault));
                    profileProperties.AlignOnSyncPoints = Int32.Parse(driverProfile.GetValue(DriverProgId, AlignOnSyncPointsName, string.Empty, AlignOnSyncPointsDefault));
                    profileProperties.SetParkLoc = Convert.ToBoolean(driverProfile.GetValue(DriverProgId, SetParkLocName, string.Empty, SetParkLocDefault));
                    profileProperties.ParkLoc = Convert.ToBoolean(driverProfile.GetValue(DriverProgId, ParkLocName, string.Empty, ParkLocDefault));
                    profileProperties.ParkLocAlt = Double.Parse(driverProfile.GetValue(DriverProgId, ParkLocAltName, string.Empty, ParkLocAltDefault));
                    profileProperties.ParkLocAz = Double.Parse(driverProfile.GetValue(DriverProgId, ParkLocAzName, string.Empty, ParkLocAzDefault));
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

                    driverProfile.WriteValue(DriverProgId, traceStateProfileName, profileProperties.TraceLogger.ToString());
                    if (!(profileProperties.ComPort is null)) driverProfile.WriteValue(DriverProgId, comPortProfileName, profileProperties.ComPort);
                    driverProfile.WriteValue(DriverProgId, siteElevationProfileName, profileProperties.SiteElevation.ToString());
                    driverProfile.WriteValue(DriverProgId, SlewSettleTimeName, profileProperties.SlewSettleTime.ToString());
                    driverProfile.WriteValue(DriverProgId, SiteLatitudeName, profileProperties.SiteLatitude.ToString());
                    driverProfile.WriteValue(DriverProgId, SiteLongitudeName, profileProperties.SiteLongitude.ToString());
                    driverProfile.WriteValue(DriverProgId, SyncTimeOnConnectName, profileProperties.SyncTimeOnConnect.ToString());
                    driverProfile.WriteValue(DriverProgId, GuideCompName, profileProperties.GuideComp.ToString());
                    driverProfile.WriteValue(DriverProgId, GuideCompMaxDeltaName, profileProperties.GuideCompMaxDelta.ToString());
                    driverProfile.WriteValue(DriverProgId, GuideCompBufferName, profileProperties.GuideCompBuffer.ToString());
                    driverProfile.WriteValue(DriverProgId, PulseGuideEquFrameName, profileProperties.PulseGuideEquFrame.ToString());
                    driverProfile.WriteValue(DriverProgId, DriverSiteOverrideName, profileProperties.DriverSiteOverride.ToString());
                    driverProfile.WriteValue(DriverProgId, DriverSiteLatitudeName, profileProperties.DriverSiteLatitude.ToString());
                    driverProfile.WriteValue(DriverProgId, DriverSiteLongitudeName, profileProperties.DriverSiteLongitude.ToString());
                    driverProfile.WriteValue(DriverProgId, PulseGuideDurationSynchronousName, profileProperties.PulseGuideDurationSynchronous.ToString());
                    driverProfile.WriteValue(DriverProgId, AlignOnSyncEnabledName, profileProperties.AlignOnSyncEnabled.ToString());
                    driverProfile.WriteValue(DriverProgId, AlignOnSyncPointsName, profileProperties.AlignOnSyncPoints.ToString());
                    driverProfile.WriteValue(DriverProgId, SetParkLocName, profileProperties.SetParkLoc.ToString());
                    driverProfile.WriteValue(DriverProgId, ParkLocName, profileProperties.ParkLoc.ToString());
                    driverProfile.WriteValue(DriverProgId, ParkLocAltName, profileProperties.ParkLocAlt.ToString());
                    driverProfile.WriteValue(DriverProgId, ParkLocAzName, profileProperties.ParkLocAz.ToString());
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
