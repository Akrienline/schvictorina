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

        } 
    }
}
