using System;
using System.Linq;
using SchVictorina.Engines;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using System.Collections.Generic;
using System.Text;

namespace SchVictorina.Utilites
{
    internal static class ConvertUtilites
    {
        public static string GetTextEngineByQuery(string query)
        {
            var querySplit = query.Split('-');
            return querySplit[0];
        }

        public static ChatId GetChatId(this Update update)
        {
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery)
                return update.CallbackQuery.Message.Chat.Id;
            else if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
                return update.Message.Chat.Id;
            else 
                return null;
        }
        public static string FindSubstring(this string text, string from, string to)
        {
            var startIndex = text.IndexOf(from);
            var endIndex = text.IndexOf(to, startIndex);
            startIndex += from.Length;
            var result = text[startIndex..endIndex];
            return result;
        }
        public static InlineKeyboardMarkup FromAnswerOptionsToKeyboardMarkup(TaskInfo question, BaseEngine baseEngine)
        {
            var apiName = ((EngineAttribute)baseEngine.GetType().GetCustomAttributes(typeof(EngineAttribute), true)[0]).ApiName;

            return new[]
            {
                question.AnswerOptions != null && question.AnswerOptions.Any()
                    ? question.AnswerOptions.Select(option =>
                    {
                        return InlineKeyboardButton.WithCallbackData(option.ToString(), $"{apiName}-{question.RightAnswer}.{option}");
                    })
                    : null,
                new []
                {
                    InlineKeyboardButton.WithCallbackData("Выйти", $"{apiName}-mainmenu"),
                    InlineKeyboardButton.WithCallbackData("Пропустить", $"{apiName}-skip")
                }
            }.Where(x => x != null).ToArray();
        }
        public static string RemoveEngineAlias(string query)
        {
            var preParsedQueryA = query.Split('-');
            return query[preParsedQueryA[1].Length..];
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
