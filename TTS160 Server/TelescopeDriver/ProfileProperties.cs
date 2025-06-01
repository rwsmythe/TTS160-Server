using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.TTS160
{
    public class ProfileProperties
    {
        public bool TraceLogger { get; set; }
        public string ComPort { get; set; }
        public double SiteElevation {  get; set; }
        public short SlewSettleTime { get; set; }
        public double SiteLatitude { get; set; }
        public double SiteLongitude { get; set; }
        public bool SyncTimeOnConnect { get; set; }
        public int GuideComp { get; set; }  //Improves guiding by compensating pulse length and/or pulse direction
        public int GuideCompMaxDelta { get; set; }
        public int GuideCompBuffer { get; set; }
        public bool PulseGuideEquFrame { get; set; }  //Test function to convert pulses into the TopoEquatorial frame from the body frame (making pulseguiding ASCOM compliant)
        public bool DriverSiteOverride { get; set; }  //Adds additional site location precision
        public double DriverSiteLatitude { get; set; }
        public double DriverSiteLongitude { get; set; }
        public bool PulseGuideDurationSynchronous { get; set; }
        public bool AlignOnSyncEnabled { get; set; }
        public int AlignOnSyncPoints { get; set; }
        public bool SetParkLoc { get; set; }
        public bool ParkLoc { get; set; }
        public int ParkLocAlt { get; set; }
        public int ParkLocAz { get; set; }
            
    }
}
