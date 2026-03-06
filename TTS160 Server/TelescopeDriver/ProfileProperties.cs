using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.TTS160
{
    /// <summary>
    /// Data class holding all driver settings persisted to the ASCOM Profile (Windows Registry).
    /// Read on driver initialization by <see cref="Telescope.TelescopeHardware.ReadProfile"/>
    /// and written by <see cref="Telescope.TelescopeHardware.WriteProfile"/> and the setup dialog.
    /// </summary>
    public class ProfileProperties
    {
        /// <summary>Whether ASCOM trace logging is enabled for diagnostic output.</summary>
        public bool TraceLogger { get; set; }

        /// <summary>COM port name for the serial connection (e.g., "COM3").</summary>
        public string ComPort { get; set; }

        /// <summary>Observing site elevation in meters above sea level.</summary>
        public double SiteElevation {  get; set; }

        /// <summary>Post-slew settle time in seconds (0–99). Default 2.</summary>
        public short SlewSettleTime { get; set; }

        /// <summary>Site latitude in degrees, as read from the mount. Sentinel value 100 = not yet read.</summary>
        public double SiteLatitude { get; set; }

        /// <summary>Site longitude in degrees, as read from the mount. Sentinel value 200 = not yet read.</summary>
        public double SiteLongitude { get; set; }

        /// <summary>Whether to synchronize the PC clock to the mount's time on connect.</summary>
        public bool SyncTimeOnConnect { get; set; }

        /// <summary>Guide compensation mode: 0 = none, 1 = altitude compensation for pulse guide corrections.</summary>
        public int GuideComp { get; set; }

        /// <summary>Maximum altitude delta (degrees) for guide compensation to apply.</summary>
        public int GuideCompMaxDelta { get; set; }

        /// <summary>Buffer zone (degrees) around the guide compensation threshold.</summary>
        public int GuideCompBuffer { get; set; }

        /// <summary>When true, converts pulse guide commands from body frame to topocentric equatorial frame (ASCOM-compliant).</summary>
        public bool PulseGuideEquFrame { get; set; }

        /// <summary>When true, uses driver-side site coordinates instead of mount-reported values for higher precision.</summary>
        public bool DriverSiteOverride { get; set; }

        /// <summary>Driver-side site latitude in degrees (used when <see cref="DriverSiteOverride"/> is true).</summary>
        public double DriverSiteLatitude { get; set; }

        /// <summary>Driver-side site longitude in degrees (used when <see cref="DriverSiteOverride"/> is true).</summary>
        public double DriverSiteLongitude { get; set; }

        /// <summary>When true, PulseGuide blocks for the full guide duration before returning.</summary>
        public bool PulseGuideDurationSynchronous { get; set; }

        /// <summary>Whether the Align-on-Sync multi-point alignment feature is enabled.</summary>
        public bool AlignOnSyncEnabled { get; set; }

        /// <summary>Number of alignment points for Align-on-Sync: 1, 2, or 3.</summary>
        public int AlignOnSyncPoints { get; set; }

        /// <summary>When true, updates the custom park position on the next Park command.</summary>
        public bool SetParkLoc { get; set; }

        /// <summary>When true, uses a custom park location; when false, parks in place.</summary>
        public bool ParkLoc { get; set; }

        /// <summary>Custom park position altitude in degrees.</summary>
        public double ParkLocAlt { get; set; }

        /// <summary>Custom park position azimuth in degrees.</summary>
        public double ParkLocAz { get; set; }

    }
}
