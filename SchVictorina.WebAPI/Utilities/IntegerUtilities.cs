using System;

namespace SchVictorina.WebAPI.Utilities
{
    public static class IntegerUtilities
    {
        public static int ToInt(this string s)
        {
            if (string.IsNullOrEmpty(s))
                throw new ArgumentNullException(nameof(s));
            return Convert.ToInt32(s);
        }
        public static int ToInt(this double d)
        {
            if (d == null)
                throw new ArgumentNullException(nameof(d));
            return Convert.ToInt32(d);
        }
    }
}
