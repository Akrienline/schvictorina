using SchVictorina.WebAPI.Utilites;
using System;
using SchVictorina.WebAPI.Utilites;
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

        public override TaskInfo GenerateQuestion()
        {
            var value1 = 0;
            var value2 = 0;
            var answer = 0;
            var tolerance1 = RandomUtilities.GetRandomInt((MaxAnswerValue - MinAnswerValue) / 10);
            var tolerance2 = RandomUtilities.GetRandomInt((MaxAnswerValue - MinAnswerValue) / 10);

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
                tolerance1 = RandomUtilities.GetRandomNonZeroInt(-2, 2);
                tolerance2 = RandomUtilities.GetRandomNonZeroInt(-3, 3);
            }

            return new TaskInfo
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

        //public TaskInfo GenerateQuestion2()
        //{
        //    var @operator = MathUtilites.GetEnumByOperator(MathUtilites.GetRandomOperator(Operators).ToString());


        //    var maxAnswerValue2 = @operator switch
        //    {
        //        Operator.Add => MaxAnswerValue / 2,
        //        Operator.Subtract => MaxAnswerValue,
        //        Operator.Multiply => Convert.ToInt32(Math.Sqrt(MaxAnswerValue)),
        //        Operator.Divide => (random.Next(10, MaxAnswerValue) / 10),
        //        _ => 0
        //    };
        //    var maxAnswerValue1 = @operator == Operator.Divide
        //        ? maxAnswerValue2 * random.Next(1, 11)
        //        : maxAnswerValue2;

        //    var value1 = @operator == Operator.Divide ? maxAnswerValue1 : random.Next(1, maxAnswerValue1);
        //    var value2 = @operator == Operator.Divide ? maxAnswerValue2 : random.Next(1, maxAnswerValue2);
        //    var answer = @operator switch
        //    {
        //        Operator.Add => value1 + value2,
        //        Operator.Subtract => value1 - value2,
        //        Operator.Multiply => value1 * value2,
        //        Operator.Divide => value1 / value2,
        //        _ => 0
        //    };
        //    return new TaskInfo
        //    {
        //        Question = @$"Сколько будет {value1}{(@operator switch
        //        {
        //            Operator.Add => "+",
        //            Operator.Subtract => "-",
        //            Operator.Multiply => "*",
        //            Operator.Divide => "/",
        //            _ => 0
        //        }
        //            )}{value2}",
        //        AnswerOptions = new object[]
        //        {
        //            answer,
        //            answer + InvertIfNeeded(random.Next(1, MaxAnswerValue / 10), RandomUtilities.GenerateBool()),
        //            answer + InvertIfNeeded(random.Next(1, MaxAnswerValue / 10), RandomUtilities.GenerateBool()),
        //        }.OrderBy(x => x).ToArray(),
        //        RightAnswer = answer
        //    };
        //}


        //private static int InvertIfNeeded(int value, bool needsInvert)
        //{
        //    return !needsInvert ? value : (-1 * value);
        //}

        //public enum Operator
        //{
        //    Unknown = 0,
        //    Add = 1,
        //    Subtract = 2,
        //    Multiply = 3,
        //    Divide = 4
        //}
    }
}
