using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

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
    }
}
