using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace SchVictorina.WebAPI
{
    public abstract class Messenger
    {
        public abstract Task SendTextMessage(string chatId, string text, List<Keybutton> keyboard);
        public abstract Task SendTextMessage(string chatId, string text);

        public abstract Task SendTextAndImage(string chatId, string text, string path, List<Keybutton> keyboard);
        public abstract Task SendTextAndImage(string chatId, string text, string path);

        internal abstract Task ProcessEvent(object obj);

        public abstract Task ProcessTextMessage();
        public abstract Task ProcessButtonMessage();
        public abstract Task ProcessCommandMessage();
    }
    public class Keybutton
    {
        public string Text;
        public string Id;
    }


    public sealed class TelegramMessenger : Messenger
    {

        private InlineKeyboardMarkup ProcessListToKeyboard(List<Keybutton> keyboard)
        {
            var buttons = new List<InlineKeyboardButton>();
            foreach (var button in keyboard)
            {
                var resultButton = new InlineKeyboardButton(button.Text);
                resultButton.CallbackData = button.Id;
                buttons.Add(resultButton);
            }
            return new InlineKeyboardMarkup(buttons); 
        }

        static TelegramBotClient botClient;
        public override Task ProcessButtonMessage()
        {
            throw new System.NotImplementedException();
        }

        public override Task ProcessCommandMessage()
        {
            throw new System.NotImplementedException();
        }

        public override Task ProcessTextMessage()
        {
            throw new System.NotImplementedException();
        }

        public override async Task SendTextAndImage(string chatId, string text, string path, List<Keybutton> keyboard)
        {
            var tgKeyboard = ProcessListToKeyboard(keyboard);
            await botClient.SendPhotoAsync(chatId, path, text, replyMarkup: tgKeyboard);
        }

        public override async Task SendTextAndImage(string chatId, string text, string path)
        {
            await botClient.SendPhotoAsync(chatId, path, text);
        }

        public override async Task SendTextMessage(string chatId, string text, List<Keybutton> keyboard)
        {
            var tgKeyboard = ProcessListToKeyboard(keyboard);
            await botClient.SendTextMessageAsync(chatId, text, replyMarkup: tgKeyboard);
        }

        public override async Task SendTextMessage(string chatId, string text)
        {
            await botClient.SendTextMessageAsync(chatId, text);
        }

        internal override Task ProcessEvent(object obj)
        {
            throw new System.NotImplementedException();
        }
    }
}
