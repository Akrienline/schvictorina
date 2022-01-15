using System;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Generic;
using System.Text;
using SchVictorina.Utilites;

namespace SchVictorina
{
    public class TaskInfo
    {
        public string Question { get; set; }
        //public string IVQuestion { get; set; }
        public object RightAnswer { get; set; }
        public object[] AnswerOptions { get; set; }
        public InlineKeyboardMarkup GetKeyboard(TaskInfo question)
        {
            return ConvertUtilites.FromAnswerOptionsToKeyboardMarkup(question);
        } 
    }
    public abstract class BaseEngine
    {
        public abstract TaskInfo GenerateQuestion();
    }
}
