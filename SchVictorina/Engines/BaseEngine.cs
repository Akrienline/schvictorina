using System;
using System.Collections.Generic;
using System.Text;

namespace SchVictorina
{
    public class TaskInfo
    {
        public string Question { get; set; }
        //public string IVQuestion { get; set; }
        public object RightAnswer { get; set; }
        public object[] AnswerOptions { get; set; }
    }
    public abstract class BaseEngine
    {
        public abstract TaskInfo GenerateQuestion();
    }
}
