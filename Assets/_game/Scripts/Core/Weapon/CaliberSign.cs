using System;

namespace Core.Weapon
{
    [Serializable]
    public struct CaliberSign : IEquatable<CaliberSign>
    {
        public int diameter;
        public int length;
        
        public float DiameterMeters => diameter / 1000f;

        /// <param name="value">example: "762x590"</param>
        public static implicit operator CaliberSign(string value)
        {
            var values = value.Split('x');
            return new CaliberSign { diameter = int.Parse(values[0]), length = int.Parse(values[1]) };
        }

        public override int GetHashCode()
        {
            return diameter ^ length;
        }
        
        public override string ToString() => $"{diameter / 10f}x{length / 10f}";

        public bool Equals(CaliberSign other)
        {
            return diameter == other.diameter && length == other.length;
        }

        public override bool Equals(object obj)
        {
            return obj is CaliberSign other && Equals(other);
        }
    }
}