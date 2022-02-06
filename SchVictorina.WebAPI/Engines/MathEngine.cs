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
                Question = @$"Сколько будет {value1} {@operator} {value2}",
                AnswerOptions = new object[]
                {
                    answer,
                    answer + tolerance1,
                    answer + tolerance2,
                }.OrderByRandom().ToArray(),
                RightAnswer = answer
            };
        }
    }
}
