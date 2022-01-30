using System;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace SchVictorina.WebAPI.Utilites
{
    public static class XmlUtilities
    {
        public static T FromXml<T>(this string xml)
            where T: class, new()
        {
            using var memoryStream = new MemoryStream();
            memoryStream.Write(Encoding.UTF8.GetBytes(xml));
            memoryStream.Position = 0;
            var serializer = new XmlSerializer(typeof(T));
            return (T)serializer.Deserialize(memoryStream);
        }
        public static string ToXml<T>(this T obj)
        {
            using var memoryStream = new MemoryStream();
            var serializer = new XmlSerializer(typeof(T));
            serializer.Serialize(memoryStream, obj);
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
    }
    public static class ConvertUtilities
    {
        public static object ParseTo(this string str, Type toType)
        {
            return Convert.ChangeType(str, toType, CultureInfo.InvariantCulture);
        }
        public static void Switch<T>(ref T obj1, ref T obj2)
        {
            T temp = obj1;
            obj1 = obj2;
            obj2 = temp;
        }
    }

    public static class RandomUtilities
    {
        private static readonly Random random = new Random();

        public static int GetRandomInt(int min, int max)
        {
            return random.Next(min, max + 1);
        }
        public static int GetRandomNonZeroInt(int min, int max)
        {
            var value = 0;
            while (value == 0)
                value = GetRandomInt(min, max);
            return value;
        }
        public static int GetRandomIndex(int maxCount)
        {
            return random.Next(0, maxCount);
        }
        public static int GetRandomPositiveInt(int maxCount)
        {
            return random.Next(1, maxCount);
        }
        public static int GetRandomInt(int max)
        {
            return random.Next(-1 * max, max + 1);
        }
        public static char GetRandomChar(this string text)
        {
            return text[GetRandomIndex(text.Length)];
        }
        public static T GetRandomValue<T>(this IReadOnlyList<T> items)
        {
            return items[GetRandomIndex(items.Count)];
        }

        public static T GetRandomEnum<T>()
        {
            var enumValues = Enum.GetValues(typeof(T)).Cast<int>().ToArray();
            return (T)(object)enumValues.GetRandomValue();
        }
        public static bool GenerateBool()
        {
            return GetRandomInt(1, 2) == 1;
        }

        public static IEnumerable<T> OrderByRandom<T>(this IEnumerable<T> items)
        {
            return items.OrderBy(x => GetRandomIndex(100000));
        }
    }
}
