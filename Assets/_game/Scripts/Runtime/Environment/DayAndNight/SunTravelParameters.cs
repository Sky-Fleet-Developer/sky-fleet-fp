using System;

namespace Runtime.Environment.DayAndNight
{
    [Serializable]
    public class SunTravelParameters
    {
        public float axisInclination;
        public float ascensionOffsetAngle;
        public float dayDurationRealtime;
        public float nightDurationRealtime;
    }
}