using SchVictorina_WebAPI;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace SchVictorina_WebAPI.Engines
{
    public class BaseButton
    {
        public string Text = "";
    }
    public sealed class GroupButton : BaseButton
    {
        public BaseButton[]? buttons;
    }
    public sealed class BasicEngineButton : BaseButton // Engine's button without any parameters
    {
        public BaseEngine? engine;
    }
    public sealed class AdvancedEngineButton : BaseButton // Engine's button with parameters.
    {
        public BaseEngine? engine;
        public Parameter[]? parameters;
    }
    public class ButtonTools
    {
        public static void Serizalize(BaseButton[] buttons, string fileName)
        {
            XmlSerializer serializer =
            new XmlSerializer(typeof(BaseButton));
            Stream fs = new FileStream(fileName, FileMode.Create);
            XmlWriter writer = new XmlTextWriter(fs, Encoding.Unicode);
            BaseButton b = new BaseButton();
            serializer.Serialize(writer, b);
            writer.Close();
        }
        public static BaseButton[] Deserialize(string fileName)
        {
            Stream fs = new FileStream(fileName, FileMode.Open);
            XmlReaderSettings settings = new();
            settings.IgnoreWhitespace = true;
            settings.Async = true;
            var reader = XmlReader.Create(fs, settings);
        }
        //public static BaseButton[] Deserialize(XmlReader xmlReader)
        //{

        //}
    }
}
