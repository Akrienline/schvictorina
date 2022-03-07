using System;
using System.Collections.Generic;
using System.Linq;

namespace SchVictorina.WebAPI
{
    public class QuestionInfo
    {
        public string Question { get; set; }
        public AnswerOption RightAnswer { get; set; }
        public AnswerOption[] WrongAnswers { get; set; }
    }

    public class AnswerOption
    {
        public AnswerOption(string text)
        {
            Text = text;
        }
        public AnswerOption(string id, string text)
        {
            ID = id;
            Text = text;
        }
        public string Text { get; private set; }
        public string ID { get; private set; }
    }

    public class AnswerInfo
    {
        public string RightAnswer { get; set; }
        public string SelectedAnswer { get; set; }
        public string Description { get; set; }
        public string DescriptionImagePath { get; set; }
    }

    public abstract class BaseEngine
    {
        public abstract QuestionInfo GenerateQuestion();
        public virtual AnswerInfo ParseAnswerId(string id) { return null; }
    }
}
