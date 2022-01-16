using System;
using System.Linq;
using SchVictorina;
using SchVictorina.Utilites;
using System.Collections.Generic;
using System.Text;

namespace SchVictorina.Engines
{
    //[Engine("equation", "Математика (уравнения")]
    public class EquationEngine : BaseEngine
    {
        public static string GetEquation(int maxValue)
        {
            var random = new Random();
            return $"x{MathUtilites.GetRandomOperator().ToString()}{random.Next(1, maxValue).ToString()}={random.Next(1, maxValue).ToString()}";
        }
        public static int RightAnswerForEquation(string equation)
        {
            var processedOperator = MathUtilites.InvertOperator(MathUtilites.GetOperator(equation));
            var trimmedEquation = equation.Substring(equation.IndexOf('=') - 1);
            var splittedEquation = trimmedEquation.Split('=');
            var int1 = Convert.ToInt32(splittedEquation[0]);
            var int2 = Convert.ToInt32(splittedEquation[1]);
            return MathUtilites.AnswerByExample(int1, processedOperator, int2);
        }
        public override TaskInfo GenerateQuestion()
        {
            var randomEquation = GetEquation(10);
            return new TaskInfo
            {
                Question = $"Чему равен {randomEquation}",
                RightAnswer = RightAnswerForEquation(randomEquation),
                AnswerOptions = MathUtilites.GetAnswerOptions(RightAnswerForEquation(randomEquation))
            };
        }
    }
}
