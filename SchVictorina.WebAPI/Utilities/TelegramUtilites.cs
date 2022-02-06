using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace SchVictorina.WebAPI.Utilities
{
    public static class TelegramUtilites
    {
        public static ChatId GetChatId(this Update update)
        {
            if (update.Type == UpdateType.CallbackQuery)
                return update.CallbackQuery?.Message?.Chat.Id;
            else if (update.Type == UpdateType.Message)
                return update.Message?.Chat.Id;
            else
                return new ChatId("");
        }

        public static User GetUser(this Update update)
        {
            if (update.Type == UpdateType.CallbackQuery)
                return update.CallbackQuery?.From;
            else if (update.Type == UpdateType.Message)
                return update.Message?.From;
            else
                return new User();
        }

        public static async Task SendText(this ITelegramBotClient botClient, Update update, string message, InlineKeyboardMarkup inlineKeyboardMarkup = null)
        {
            await botClient.SendTextMessageAsync(update.GetChatId(), message, replyMarkup: inlineKeyboardMarkup, cancellationToken: CancellationToken.None);
        }
        public static async Task SendImage(this ITelegramBotClient botClient, Update update, string filePath)
        {
            await botClient.SendPhotoAsync(update.GetChatId(), new InputOnlineFile(new MemoryStream(System.IO.File.ReadAllBytes(filePath))), cancellationToken: CancellationToken.None);
        }
        public static async Task SendTextAndImage(this ITelegramBotClient botClient, Update update, string message, string filePath)
        {
            await SendText(botClient, update, message);
            await SendImage(botClient, update, filePath);
        }
    }
}
