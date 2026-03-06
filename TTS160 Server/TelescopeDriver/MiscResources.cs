using System;
using System.Xml.Schema;
using ASCOM.DeviceInterface;

namespace ASCOM.TTS160
{
    /// <summary>
    /// Horizon (Alt/Az) coordinate pair in degrees.
    /// </summary>
    public class HorizonCoordinates
    {
        public double Altitude { get; set; }
        public double Azimuth { get; set; }
    }

    /// <summary>
    /// Equatorial coordinate pair (RA in hours, Dec in degrees).
    /// </summary>
    public class EquatorialCoordinates
    {
        public double RightAscension { get; set; }
        public double Declination { get; set; }
    }

    /// <summary>
    /// Static container for thread-safe runtime state shared across driver instances.
    /// All properties are backed by <see cref="ThreadSafeValue{T}"/> for lock-free access
    /// via <see cref="System.Threading.Interlocked.Exchange(ref object, object)"/>.
    /// </summary>
    /// <remarks>
    /// Tracks slewing, pulse-guiding, parking, targeting, MoveAxis settle, and alignment state.
    /// Written by <see cref="TelescopeHardware"/> methods; read by both hardware and driver layers.
    /// </remarks>
    internal class MiscResources
    {
        /// <summary>True while a MoveAxis command is active on the primary (RA/Az) axis.</summary>
        private static readonly ThreadSafeValue<bool> _movingPrimary = false;
        public static bool MovingPrimary
        {
            get => _movingPrimary;
            internal set => _movingPrimary.Set(value);
        }

        /// <summary>True while a MoveAxis command is active on the secondary (Dec/Alt) axis.</summary>
        private static readonly ThreadSafeValue<bool> _movingSecondary = false;
        public static bool MovingSecondary
        {
            get => _movingSecondary;
            internal set => _movingSecondary.Set(value);
        }

        /// <summary>True while a pulse guide operation is in progress on any axis.</summary>
        private static readonly ThreadSafeValue<bool> _isPulseGuiding = false;
        public static bool IsPulseGuiding
        {
            get => _isPulseGuiding;
            internal set => _isPulseGuiding.Set(value);
        }

        /// <summary>Duration of the current pulse guide command in milliseconds.</summary>
        private static readonly ThreadSafeValue<int> _PulseGuideDuration = 0;
        public static int PulseGuideDuration
        {
            get => _PulseGuideDuration;
            internal set => _PulseGuideDuration.Set(value);
        }

        /// <summary>Timestamp when the current pulse guide command started.</summary>
        private static readonly ThreadSafeValue<DateTime> _PulseGuideStart = DateTime.MinValue;
        public static DateTime PulseGuideStart
        {
            get => _PulseGuideStart;
            internal set => _PulseGuideStart.Set(value);
        }

        /// <summary>True while any slew (GoTo or MoveAxis) is in progress.</summary>
        private static readonly ThreadSafeValue<bool> _isSlewing = false;
        public static bool IsSlewing
        {
            get => _isSlewing;
            internal set => _isSlewing.Set(value);
        }

        /// <summary>True while a GoTo slew (SlewToTarget/Coordinates) is in progress, as opposed to MoveAxis.</summary>
        private static readonly ThreadSafeValue<bool> _isSlewingToTarget = false;
        public static bool IsSlewingToTarget
        {
            get => _isSlewingToTarget;
            internal set => _isSlewingToTarget.Set(value);
        }

        /// <summary>True after TargetRightAscension has been set by a client.</summary>
        private static readonly ThreadSafeValue<bool> _isTargetRASet = false;
        public static bool IsTargetRASet
        {
            get => _isTargetRASet;
            internal set => _isTargetRASet.Set(value);
        }

        /// <summary>True after TargetDeclination has been set by a client.</summary>
        private static readonly ThreadSafeValue<bool> _isTargetDecSet = false;
        public static bool IsTargetDecSet
        {
            get => _isTargetDecSet;
            internal set => _isTargetDecSet.Set(value);
        }

        /// <summary>True when both TargetRA and TargetDec have been set (ready for SlewToTarget).</summary>
        private static readonly ThreadSafeValue<bool> _isTargetSet = false;
        public static bool IsTargetSet
        {
            get => _isTargetSet;
            internal set => _isTargetSet.Set(value);
        }

        /// <summary>The client-set target coordinates (RA hours, Dec degrees) for SlewToTarget.</summary>
        private static readonly ThreadSafeValue<EquatorialCoordinates> _Target = new EquatorialCoordinates();
        public static EquatorialCoordinates Target
        {
            get => _Target;
            internal set => _Target.Set(value);

        }

        /// <summary>The active slew destination coordinates, which may differ from Target after coordinate transforms.</summary>
        private static readonly ThreadSafeValue<EquatorialCoordinates> _SlewTarget = new EquatorialCoordinates();
        public static EquatorialCoordinates SlewTarget
        {
            get => _SlewTarget;
            internal set => _SlewTarget.Set(value);

        }

        /// <summary>Post-slew settle time in seconds. Default 2. Configurable via setup dialog.</summary>
        public static readonly ThreadSafeValue<short> _SettleTime = 2;
        public static short SettleTime
        {
            get => _SettleTime;
            internal set => _SettleTime.Set(value);
        }

        /// <summary>Timestamp when the post-slew settle period began.</summary>
        private static readonly ThreadSafeValue<DateTime> _SlewSettleStart = DateTime.MinValue;
        public static DateTime SlewSettleStart
        {
            get => _SlewSettleStart;
            internal set => _SlewSettleStart.Set(value);
        }

        /// <summary>Timestamp when the East/West MoveAxis settle period began.</summary>
        private static readonly ThreadSafeValue<DateTime> _EWMoveAxisSettleStart = DateTime.MinValue;
        public static DateTime EWMoveAxisSettleStart
        {
            get => _EWMoveAxisSettleStart;
            internal set => _EWMoveAxisSettleStart.Set(value);
        }

        /// <summary>Timestamp when the North/South MoveAxis settle period began.</summary>
        private static readonly ThreadSafeValue<DateTime> _NSMoveAxisSettleStart = DateTime.MinValue;
        public static DateTime NSMoveAxisSettleStart
        {
            get => _NSMoveAxisSettleStart;
            internal set => _NSMoveAxisSettleStart.Set(value);
        }

        /// <summary>Set true to signal the East/West MoveAxis background thread to stop.</summary>
        private static readonly ThreadSafeValue<Boolean> _EWMoveAxisStopFlag = false;
        public static Boolean EWMoveAxisStopFlag
        {
            get => _EWMoveAxisStopFlag;
            internal set => _EWMoveAxisStopFlag.Set(value);
        }

        /// <summary>Set true to signal the North/South MoveAxis background thread to stop.</summary>
        private static readonly ThreadSafeValue<Boolean> _NSMoveAxisStopFlag = false;
        public static Boolean NSMoveAxisStopFlag
        {
            get => _NSMoveAxisStopFlag;
            internal set => _NSMoveAxisStopFlag.Set(value);
        }

        /// <summary>True when the mount is in the parked state. Cleared on Unpark.</summary>
        private static readonly ThreadSafeValue<bool> _IsParked = false;
        public static bool IsParked
        {
            get => _IsParked;
            internal set => _IsParked.Set(value);
        }

        /// <summary>Temporarily overrides tracking state during SlewToAltAz operations.</summary>
        private static readonly ThreadSafeValue<bool> _SlewAltAzTrackOverride = false;
        public static bool SlewAltAzTrackOverride
        {
            get => _SlewAltAzTrackOverride;
            internal set => _SlewAltAzTrackOverride.Set(value);

        }
        /// <summary>When true, the driver attempts to restore the user's tracking rate preference after operations that change it.</summary>
        private static readonly ThreadSafeValue<bool> _TrackSetFollower = true;
        public static bool TrackSetFollower
        {
            get => _TrackSetFollower;
            internal set => _TrackSetFollower.Set(value);
        }
        /// <summary>True when the mount has been homed via FindHome and not yet moved.</summary>
        private static readonly ThreadSafeValue<bool> _isAtHome = false;
        public static bool isAtHome
        {
            get => _isAtHome;
            internal set => _isAtHome.Set(value);
        }

        /// <summary>True when the Align-on-Sync feature is enabled (multi-point alignment via sync commands).</summary>
        private static readonly ThreadSafeValue<bool> _AlignOnSyncEnabled = false;
        public static bool AlignOnSyncEnabled
        {
            get => _AlignOnSyncEnabled;
            internal set => _AlignOnSyncEnabled.Set(value);
        }

        /// <summary>Number of alignment sync points remaining before Align-on-Sync completes its calibration.</summary>
        private static readonly ThreadSafeValue<int> _AlignOnSyncPoints = 0;
        public static int AlignOnSyncPoints
        {
            get => _AlignOnSyncPoints;
            internal set => _AlignOnSyncPoints.Set(value);
        }
    }
}
