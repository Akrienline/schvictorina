using System;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Generic;
using System.Text;

namespace SchVictorina.Utilites
{
    internal static class ConvertUtilites
    {
        public static string FindSubstring(this string text, string from, string to)
        {
            var startIndex = text.IndexOf(from);
            var endIndex = text.IndexOf(to, startIndex);
            startIndex += from.Length;
            var result = text.Substring(startIndex, endIndex - startIndex);
            return result;
        }
        public static InlineKeyboardMarkup FromAnswerOptionsToKeyboardMarkup(TaskInfo question)
        {
            if (question.AnswerOptions != null)
            {
                var allButtons = new List<List<InlineKeyboardButton>>();
                var buttonsRow1 = new List<InlineKeyboardButton>();
                var buttonsRow2 = new List<InlineKeyboardButton>();
                var logoutButton = InlineKeyboardButton.WithCallbackData("Выйти", "logout");
                buttonsRow1.Add(logoutButton);
                foreach (var option in question.AnswerOptions)
                {
                    buttonsRow2.Add(InlineKeyboardButton.WithCallbackData(option.ToString(), question.RightAnswer + "." + option.ToString()));
                }
                IEnumerable<InlineKeyboardButton> row1 = buttonsRow1;
                IEnumerable<InlineKeyboardButton> row2 = buttonsRow2;
                IEnumerable<IEnumerable<InlineKeyboardButton>> allRows = new[] { row1, row2 };
                return new InlineKeyboardMarkup(allRows);
            }
            else
            {
                var logoutButton = InlineKeyboardButton.WithCallbackData("Выйти", "logout");
                return new InlineKeyboardMarkup(logoutButton);
            }
        }
        public static bool FromCallbackQueryToTrueOrFalse(string query)
        {
            var parsedQuery = query.Split('.');
            var rightAnswer = parsedQuery[0];
            var userAnswer = parsedQuery[1];
            if (rightAnswer == userAnswer)
                return true;
            else
                return false;
        }
    }
}
