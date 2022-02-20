using System;
using System.Collections.Generic;
using System.Linq;

namespace SchVictorina.WebAPI
{
    public class QuestionInfo
    {
        public string? Question { get; set; }
        public object? RightAnswer { get; set; }
        public object[]? WrongAnswers { get; set; }
    }

    public abstract class BaseEngine
    {
        public abstract QuestionInfo GenerateQuestion();
    }
    public abstract class Parameter
    {
        string Name = "";
        object? Value = "";
    }
}
