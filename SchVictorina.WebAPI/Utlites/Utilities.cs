using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Serialization;

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
    }
}
