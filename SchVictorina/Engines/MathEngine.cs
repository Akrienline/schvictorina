using System;
using System.Linq;

namespace SchVictorina.Engines
{
    public class MathEngine : BaseEngine
    {
        private static Random random = new Random();
        public int MaxAnswerValue = 100;

        public override TaskInfo GenerateQuestion()
        {
            var @operator = GenerateEnum<Operator>();

            var maxAnswerValue2 = @operator switch
            {
                Operator.Add => MaxAnswerValue / 2,
                Operator.Subtract => MaxAnswerValue,
                Operator.Multiply => Convert.ToInt32(Math.Sqrt(MaxAnswerValue)),
                Operator.Divide => (random.Next(10, MaxAnswerValue) / 10),
                _ => 0
            };
            var maxAnswerValue1 = @operator == Operator.Divide
                ? maxAnswerValue2 * random.Next(1, 11)
                : maxAnswerValue2;

            var value1 = @operator == Operator.Divide ? maxAnswerValue1 : random.Next(1, maxAnswerValue1);
            var value2 = @operator == Operator.Divide ? maxAnswerValue2 : random.Next(1, maxAnswerValue2);
            var answer = @operator switch
            {
                Operator.Add => value1 + value2,
                Operator.Subtract => value1 - value2,
                Operator.Multiply => value1 * value2,
                Operator.Divide => value1 / value2,
                _ => 0
            };
            return new TaskInfo
            {
                Question = @$"Сколько будет {value1}{(@operator switch
                {
                    Operator.Add => "+",
                    Operator.Subtract => "-",
                    Operator.Multiply => "*",
                    Operator.Divide => "/",
                    _ => 0
                }
                    )}{value2}",
                //IVQuestion = $"mathengine-{value1}{(@operator switch { Operator.Add => "+", Operator.Subtract => "-", Operator.Multiply => "*", Operator.Divide => "/", _ => 0 })}{value2}",
                AnswerOptions = new object[]
                {
                    answer,
                    answer + InvertIfNeeded(random.Next(1, MaxAnswerValue / 10), GenerateBool()),
                    answer + InvertIfNeeded(random.Next(1, MaxAnswerValue / 10), GenerateBool()),
                }.OrderBy(x => x).ToArray(),
                RightAnswer = answer
            };
        }

        private static bool GenerateBool()
        {
            return random.Next(1, 3) == 1;
        }
        private static int InvertIfNeeded(int value, bool needsInvert)
        {
            return !needsInvert ? value : (-1 * value);
        }

        private static T GenerateEnum<T>()
        {
            var enumValues = Enum.GetValues(typeof(T)).Cast<int>().ToArray();
            var index = random.Next(0, enumValues.Length);
            return (T)(object)enumValues[index];
        }

        public enum Operator
        {
            Add,
            Subtract,
            Multiply,
            Divide
        }
    }
}
