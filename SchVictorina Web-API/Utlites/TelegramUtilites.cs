using System;
using SchVictorina_WebAPI;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace SchVictorina_WebAPI.Utilites
{
    public static class TelegramUtilites
    {
        public static InlineKeyboardMarkup FromAnswerOptionsToKeyboardMarkup(TaskInfo question, BaseEngine baseEngine)
        {
            var apiName = ((EngineAttribute)baseEngine.GetType().GetCustomAttributes(typeof(EngineAttribute), true)[0]).ApiName;

            return new []
            {
                question.AnswerOptions != null && question.AnswerOptions.Any()
                    ? question.AnswerOptions.Select(option =>
                    {
                        return InlineKeyboardButton.WithCallbackData(option?.ToString() ?? "", $"{apiName}-{question.RightAnswer}.{option}");
                    }).ToArray()
                    : Array.Empty<InlineKeyboardButton>(),
                new []
                {
                    InlineKeyboardButton.WithCallbackData("Выйти", $"{apiName}-mainmenu"),
                    InlineKeyboardButton.WithCallbackData("Пропустить", $"{apiName}-skip")
                }
            }.Where(x => x != null && x.Length > 0).ToArray();
        }
        public static ChatId? GetChatId(this Update update)
        {
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery)
                return update.CallbackQuery?.Message?.Chat.Id;
            else if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
                return update.Message?.Chat.Id;
            else
                return null;
        }
        public static bool FromCallbackQueryToTrueOrFalse(string query)
        {
            var parsedQuery = query[(query.IndexOf('-') + 1)..].Split('.');
            var rightAnswer = parsedQuery[0];
            var userAnswer = parsedQuery[1];
            if (rightAnswer == userAnswer)
                return true;
            else
                return false;
        }
    }
}
