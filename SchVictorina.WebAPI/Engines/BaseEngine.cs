using System;
using System.Collections.Generic;
using System.Linq;

namespace SchVictorina.WebAPI
{
    public class TaskInfo
    {
        public string? Question { get; set; }
        public object? RightAnswer { get; set; }
        public object[]? AnswerOptions { get; set; }
    }

    public abstract class BaseEngine
    {
        public abstract TaskInfo GenerateQuestion();
    }
    public abstract class Parameter
    {
        string Name = "";
        object? Value = "";
    }
}
