using SchVictorina.WebAPI.Utilities;
using System;
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
        public int Depth { get; set; } = 0;

        public override QuestionInfo GenerateQuestion()
        {
            var value1 = 0;
            var value2 = 0;
            var answer = 0;
            var tolerance1 = RandomUtilities.GetRandomInt((MaxAnswerValue - MinAnswerValue) / 10, new int[] { 0 });
            var tolerance2 = RandomUtilities.GetRandomInt((MaxAnswerValue - MinAnswerValue) / 10, new int[] { 0, tolerance1 });

            var @operator = RandomUtilities.GetRandomChar(Operators);
            if (@operator == '+')
            {
                value1 = RandomUtilities.GetRandomInt(MinAnswerValue, MaxAnswerValue / 2);
                value2 = RandomUtilities.GetRandomInt(MinAnswerValue, MaxAnswerValue / 2);
                answer = value1 + value2;
            }
            else if (@operator == '-')
            {
                value1 = RandomUtilities.GetRandomInt(MinAnswerValue, MaxAnswerValue);
                value2 = RandomUtilities.GetRandomInt(MinAnswerValue, MaxAnswerValue);
                if (!AllowNegative && value1 < value2)
                    ConvertUtilities.Switch(ref value1, ref value2);
                answer = value1 - value2;
            }
            else if (@operator == '*')
            {
                value1 = RandomUtilities.GetRandomInt(Convert.ToInt32(Math.Sqrt(MinAnswerValue)), Convert.ToInt32(Math.Sqrt(MaxAnswerValue)));
                value2 = RandomUtilities.GetRandomInt(Convert.ToInt32(Math.Sqrt(MinAnswerValue)), Convert.ToInt32(Math.Sqrt(MaxAnswerValue)));
                answer = value1 * value2;
            }
            else if (@operator == '/')
            {
                value2 = 1 + RandomUtilities.GetRandomInt(MinAnswerValue, MaxAnswerValue) / 10;
                value1 = value2 * RandomUtilities.GetRandomPositiveInt(10);
                answer = value1 / value2;
                tolerance1 = RandomUtilities.GetRandomInt(-2, 2, new int[] { 0 });
                tolerance2 = RandomUtilities.GetRandomInt(-3, 3, new int[] { 0, tolerance1 });
            }

            return new QuestionInfo
            {
                Question = @$"Сколько будет {GetExpression(value1, Depth)} {@operator} {GetExpression(value2, Depth)}",
                RightAnswer = answer,
                WrongAnswers = new object[]
                {
                    answer + tolerance1,
                    answer + tolerance2,
                },
                
            };
        }
        public string GetExpression(int answer, int depth)
        {
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
            return $"( {GetExpression(value1, depth)} {@operator} {GetExpression(value2, depth)} )";
        }
    }
}
