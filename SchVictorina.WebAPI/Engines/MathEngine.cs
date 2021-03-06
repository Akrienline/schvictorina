using SchVictorina.WebAPI.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SchVictorina.WebAPI.Engines
{
    public class MathEngine : BaseEngine
    {
        private static readonly Random random = new Random();
        public int MinAnswerValue { get; set; } = 0;
        public int MaxAnswerValue { get; set; } = 100;
        public string Operators { get; set; }= "+-*/";
        public bool AllowNegative { get; set; } = true;
        public int MinDepth { get; set; } = 0;
        public int MaxDepth { get; set; } = 0;
        public int WrongAnswerCount { get; set; } = 2;

        public override QuestionInfo GenerateQuestion()
        {
            var value1 = 0; //первое число
            var value2 = 0; //второе число
            var answer = 0; //ответ
            var tolerances = ConvertUtilities.SequenceElements(WrongAnswerCount, new[] { 0 }, previous => RandomUtilities.GetRandomInt((MaxAnswerValue - MinAnswerValue) / 10, previous)).ToArray();

            var @operator = RandomUtilities.GetRandomChar(Operators);
            if (@operator == '+')
            {
                value1 = RandomUtilities.GetRandomInt(MinAnswerValue, MaxAnswerValue / 2); //генерируем два числа
                value2 = RandomUtilities.GetRandomInt(MinAnswerValue, MaxAnswerValue / 2);
                answer = value1 + value2;
            }
            else if (@operator == '-')
            {
                value1 = RandomUtilities.GetRandomInt(MinAnswerValue, MaxAnswerValue); //генерация двух чисел
                value2 = RandomUtilities.GetRandomInt(MinAnswerValue, MaxAnswerValue);
                if (!AllowNegative && value1 < value2) // если негативные числа запрещены, то меняем местами
                    ConvertUtilities.Switch(ref value1, ref value2);
                answer = value1 - value2;
            }
            else if (@operator == '*')
            {
                value1 = RandomUtilities.GetRandomInt(Convert.ToInt32(Math.Sqrt(MinAnswerValue)), Convert.ToInt32(Math.Sqrt(MaxAnswerValue))); //генерируем два числа, и в квадратный корень
                value2 = RandomUtilities.GetRandomInt(Convert.ToInt32(Math.Sqrt(MinAnswerValue)), Convert.ToInt32(Math.Sqrt(MaxAnswerValue)));
                answer = value1 * value2;
            }
            else if (@operator == '/') //деление через умножение
            {
                value2 = 1 + RandomUtilities.GetRandomInt(MinAnswerValue, MaxAnswerValue) / 10;
                value1 = value2 * RandomUtilities.GetRandomPositiveInt(10);
                answer = value1 / value2;
                tolerances = ConvertUtilities.SequenceElements(WrongAnswerCount, new[] { 0 }, previous => RandomUtilities.GetRandomInt(-2 - previous.Length + 1, 2 + previous.Length - 1, previous)).ToArray();
            }
            
            var depth = RandomUtilities.GetRandomInt(MinDepth, MaxDepth);
            var expression1 = GetExpression(value1, depth);
            var expression2 = GetExpression(value2, depth);
            var fullExpression = MathUtilities.GetPositiveExpression(expression1, expression2, @operator);
            return new QuestionInfo
            {
                Question = @$"Сколько будет {Environment.NewLine}{fullExpression}",
                RightAnswer =  new AnswerOption(answer.ToString()),
                WrongAnswers = tolerances.Select(tolerance => answer + tolerance).Select(x => new AnswerOption(x.ToString())).ToArray()
            };
        }
        public string GetExpression(int answer, int depth)
        {
            if (RandomUtilities.GetRandomInt(1, 3) == 3)
                --depth;
            if (depth <= 0)
                return answer.ToString();
            if (answer == 0)
                return answer.ToString();

            var value1 = 0;
            var value2 = 0;
            var @operator = RandomUtilities.GetRandomChar(Operators);
            if (@operator == '+')
            {
                value1 = RandomUtilities.GetRandomInt(MinAnswerValue, answer);
                value2 = answer - value1;
                if (value1 == 0 || value2 == 0)
                    return answer.ToString();
            }
            else if (@operator == '-')
            {
                value1 = RandomUtilities.GetRandomInt(MinAnswerValue, answer);
                value2 = value1 - answer;
            }
            else if (@operator == '*')
            {
                for (var v = 100; v >= 1; --v)
                {
                    if ((answer / v) == (answer / Convert.ToDouble(v)))
                    {
                        value1 = answer / v;
                        value2 = v;
                        break;
                    }
                }
                if (value1 == 1 || value2 == 1)
                    return answer.ToString();
            }
            else if (@operator == '/')
            {
                for (var v = 10; v >= 1; --v)
                {
                    if (answer * v > MaxAnswerValue)
                        continue;
                    if ((answer / v) == (answer / Convert.ToDouble(v)))
                    {
                        value1 = answer * v;
                        value2 = v;
                        break;
                    }
                }
                if (value1 == 1 || value2 == 1)
                    return answer.ToString();
            }
            --depth;
            var expression1 = GetExpression(value1, depth);
            var expression2 = GetExpression(value2, depth);
            var fullExpressison = MathUtilities.GetPositiveExpression(expression1, expression2, @operator);
            return $"({fullExpressison})";
        }
    }
}
