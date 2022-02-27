using System;

namespace SchVictorina.WebAPI.Utilities
{
    public static class IntegerUtilities
    {
        #region Int
        public static double ToDouble(this int i)
        {
            return Convert.ToDouble(i);
        }
        public static double ToDouble(this int? i)
        {
            if (i == null)
                throw new ArgumentNullException(nameof(i));
            return Convert.ToDouble(i);
        }
        public static float ToFloat(this int i)
        {
            return Convert.ToSingle(i);
        }
        public static float ToFloat(this int? i)
        {
            if (i == null)
                throw new ArgumentNullException(nameof(i));
            return Convert.ToSingle(i);
        }
        public static float ToLong(this int i)
        {
            return long.Parse(i.ToString());
        }
        public static float ToLong(this int? i)
        {
            if (i == null)
                throw new ArgumentNullException(nameof(i));
            return long.Parse(i.ToString());
        }
        public static short ToShort(this int i)
        {
            return short.Parse(i.ToString());
        }
        public static short ToShort(this int? i)
        {
            if (i == null)
                throw new ArgumentNullException(nameof(i));
            return short.Parse(i.ToString());
        }
        #endregion
        #region Double
        public static double ToInt(this double d)
        {
            return Convert.ToInt32(d);
        }
        public static double ToInt(this double? d)
        {
            if (d == null)
                throw new ArgumentNullException(nameof(d));
            return Convert.ToInt32(d);
        }
        public static float ToFloat(this double d)
        {
            return Convert.ToSingle(d);
        }
        public static float ToFloat(this double? d)
        {
            if (d == null)
                throw new ArgumentNullException(nameof(d));
            return Convert.ToSingle(d);
        }
        public static float ToLong(this double d)
        {
            return long.Parse(d.ToString());
        }
        public static float ToLong(this double? d)
        {
            if (d == null)
                throw new ArgumentNullException(nameof(d));
            return long.Parse(d.ToString());
        }
        public static short ToShort(this double d)
        {
            return short.Parse(d.ToString());
        }
        public static short ToShort(this double? d)
        {
            if (d == null)
                throw new ArgumentNullException(nameof(d));
            return short.Parse(d.ToString());
        }
        #endregion
    }
}
