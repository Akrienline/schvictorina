using SchVictorina.WebAPI;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Telegram.Bot.Types.ReplyMarkups;

namespace SchVictorina.WebAPI.Engines
{
    public class BaseButton
    {
        public string Text = "";
        public string CallbackData = "";
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
            XmlSerializer serializer = new
            XmlSerializer(typeof(BaseButton[]));
            var reader = XmlReader.Create(fs, settings);
            return (BaseButton[])serializer.Deserialize(fs);
        }
        //public static Task<InlineKeyboardMarkup> GetInlineKeyboard(BaseButton[] buttons)
        //{
            
        //}
        //public static Task<InlineKeyboardMarkup> GetInlineKeyboard(string filename)
        //{
        //    return GetInlineKeyboard(Deserialize(filename));
        //}
        //public static Task<InlineKeyboardButton> GetInlineButton(BaseButton button)
        //{
            
        //}
        //public static BaseButton[] Deserialize(XmlReader xmlReader)
        //{

        //}
    }
}
