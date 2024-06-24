using System;
using System.Xml.Schema;
using ASCOM.DeviceInterface;

namespace ASCOM.TTS160
{
    public class HorizonCoordinates
    {
        public double Altitude { get; set; }
        public double Azimuth { get; set; }
    }
    public class EquatorialCoordinates
    {
        public double RightAscension { get; set; }
        public double Declination { get; set; }
    }
    internal class MiscResources
    {
        private static readonly ThreadSafeValue<bool> _movingPrimary = false;
        public static bool MovingPrimary
        {
            get => _movingPrimary;
            internal set => _movingPrimary.Set(value);
        }

        private static readonly ThreadSafeValue<bool> _movingSecondary = false;
        public static bool MovingSecondary
        {
            get => _movingSecondary;
            internal set => _movingSecondary.Set(value);
        }

        private static readonly ThreadSafeValue<bool> _isPulseGuiding = false;
        public static bool IsPulseGuiding
        {
            get => _isPulseGuiding;
            internal set => _isPulseGuiding.Set(value);
        }

        private static readonly ThreadSafeValue<bool> _EWPulseGuideFlag = false;
        public static bool EWPulseGuideFlag
        {
            get => _EWPulseGuideFlag;
            internal set => _EWPulseGuideFlag.Set(value);
        }

        private static readonly ThreadSafeValue<bool> _NSPulseGuideFlag = false;
        public static bool NSPulseGuideFlag
        {
            get => _NSPulseGuideFlag;
            internal set => _NSPulseGuideFlag.Set(value);
        }


        private static readonly ThreadSafeValue<DateTime> _EWPulseGuideFinish = DateTime.MinValue;
        public static DateTime EWPulseGuideFinish
        {
            get => _EWPulseGuideFinish;
            internal set => _EWPulseGuideFinish.Set(value);
        }

        private static readonly ThreadSafeValue<DateTime> _NSPulseGuideFinish = DateTime.MinValue;
        public static DateTime NSPulseGuideFinish
        {
            get => _NSPulseGuideFinish;
            internal set => _NSPulseGuideFinish.Set(value);
        }

        private static readonly ThreadSafeValue<bool> _isSlewing = false;
        public static bool IsSlewing
        {
            get => _isSlewing;
            internal set => _isSlewing.Set(value);
        }

        private static readonly ThreadSafeValue<bool> _isSlewingToTarget = false;
        public static bool IsSlewingToTarget
        {
            get => _isSlewingToTarget;
            internal set => _isSlewingToTarget.Set(value);
        }

        private static readonly ThreadSafeValue<bool> _isTargetRASet = false;
        public static bool IsTargetRASet
        {
            get => _isTargetRASet;
            internal set => _isTargetRASet.Set(value);
        }

        private static readonly ThreadSafeValue<bool> _isTargetDecSet = false;
        public static bool IsTargetDecSet
        {
            get => _isTargetDecSet;
            internal set => _isTargetDecSet.Set(value);
        }

        private static readonly ThreadSafeValue<bool> _isTargetSet = false;
        public static bool IsTargetSet
        {
            get => _isTargetSet;
            internal set => _isTargetSet.Set(value);
        }

        private static readonly ThreadSafeValue<EquatorialCoordinates> _Target = new EquatorialCoordinates();
        public static EquatorialCoordinates Target
        {
            get => _Target;
            internal set => _Target.Set(value);

        }

        private static readonly ThreadSafeValue<EquatorialCoordinates> _SlewTarget = new EquatorialCoordinates();
        public static EquatorialCoordinates SlewTarget
        {
            get => _SlewTarget;
            internal set => _SlewTarget.Set(value);

        }

        public static readonly ThreadSafeValue<short> _SettleTime = 2;
        public static short SettleTime
        {
            get => _SettleTime;
            internal set => _SettleTime.Set(value);
        }
        private static readonly ThreadSafeValue<Boolean> _isSlewingAsync = false;
        public static Boolean IsSlewingAsync
        {
            get => _isSlewingAsync;
            internal set => _isSlewingAsync.Set(value);
        }

        private static readonly ThreadSafeValue<DateTime> _SlewSettleStart = DateTime.MinValue;
        public static DateTime SlewSettleStart
        {
            get => _SlewSettleStart;
            internal set => _SlewSettleStart.Set(value);
        }

        private static readonly ThreadSafeValue<DateTime> _EWMoveAxisSettleStart = DateTime.MinValue;
        public static DateTime EWMoveAxisSettleStart
        {
            get => _EWMoveAxisSettleStart;
            internal set => _EWMoveAxisSettleStart.Set(value);
        }

        private static readonly ThreadSafeValue<DateTime> _NSMoveAxisSettleStart = DateTime.MinValue;
        public static DateTime NSMoveAxisSettleStart
        {
            get => _NSMoveAxisSettleStart;
            internal set => _NSMoveAxisSettleStart.Set(value);
        }

        private static readonly ThreadSafeValue<Boolean> _EWMoveAxisStopFlag = false;
        public static Boolean EWMoveAxisStopFlag
        {
            get => _EWMoveAxisStopFlag;
            internal set => _EWMoveAxisStopFlag.Set(value);
        }

        private static readonly ThreadSafeValue<Boolean> _NSMoveAxisStopFlag = false;
        public static Boolean NSMoveAxisStopFlag
        {
            get => _NSMoveAxisStopFlag;
            internal set => _NSMoveAxisStopFlag.Set(value);
        }

        private static readonly ThreadSafeValue<DriveRates> _TrackingRateCurrent = DriveRates.driveSidereal;
        public static DriveRates TrackingRateCurrent
        {
            get => _TrackingRateCurrent;
            internal set => _TrackingRateCurrent.Set(value);

        }

        private static readonly ThreadSafeValue<bool> _IsParked = false;
        public static bool IsParked
        {
            get => _IsParked;
            internal set => _IsParked.Set(value);
        }

        private static readonly ThreadSafeValue<bool> _SlewAltAzTrackOverride = false;
        public static bool SlewAltAzTrackOverride
        {
            get => _SlewAltAzTrackOverride;
            internal set => _SlewAltAzTrackOverride.Set(value);

        }
        private static readonly ThreadSafeValue<bool> _TrackSetFollower = true; //try to follow the track setting
        public static bool TrackSetFollower
        {
            get => _TrackSetFollower;
            internal set => _TrackSetFollower.Set(value);
        }
    }
}
