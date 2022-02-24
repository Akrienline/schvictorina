using System;
using System.Collections.Generic;
using System.Linq;

namespace SchVictorina.WebAPI
{
    public class QuestionInfo
    {
        #nullable enable
        public string? Question { get; set; }
        public object? RightAnswer { get; set; }
        public object[]? WrongAnswers { get; set; }
        #nullable disable
    }

    public abstract class BaseEngine
    {
        public abstract QuestionInfo GenerateQuestion();
    }
    public abstract class Parameter
    {
        
        string Name = "";
#nullable enable
        object? Value = "";
#nullable disable
    }
}
