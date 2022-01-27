using System;
using static SchVictorina.WebAPI.Engines.MathEngine;

namespace SchVictorina.WebAPI.Utilites
{
    public class MathUtilites
    {
        public static char GetRandomOperator(string? operators = null)
        {
            var randomizer = new Random();
            if (operators != null)
            {
                var randomInt = randomizer.Next(1, operators.Length + 1);
                return randomInt switch
                {
                    1 => '+',
                    2 => '-',
                    3 => '*',
                    4 => ':',
                    _ => ' ',
                };
            }
            else
            {
                var randomInt = randomizer.Next(1, 5);
                return randomInt switch
                {
                    1 => '+',
                    2 => '-',
                    3 => '*',
                    4 => ':',
                    _ => ' ',
                };
            }
        }
        public static char GetOperator(string example)
        {
            if (example.Contains('+'))
                return '+';
            else if (example.Contains('-'))
                return '-';
            else if (example.Contains('*'))
                return '*';
            else
                return ':';
        }
        public static Operator GetEnumByOperator(string @operator)
        {
            return @operator switch
            {
                "+" => Operator.Add,
                "-" => Operator.Subtract,
                "*" => Operator.Multiply,
                "/" => Operator.Divide,
                _ => Operator.Unknown
            };
        }
        public static Operator GetOperatorEnum(char @operator)
        {
            return @operator switch
            {
                '+' => Operator.Add,
                '-' => Operator.Subtract,
                '*' => Operator.Multiply,
                '/' => Operator.Divide,
                _ => Operator.Unknown
            };
        }
        public static char InvertOperator(char @operator)
        {
            if (@operator == '+')
                return '-';
            else if (@operator == '-')
                return '+';
            else if (@operator == '*')
                return ':';
            else
                return '*';
        }
        public static char InvertOperator(string example)
        {
            return InvertOperator(GetOperator(example));
        }
        public static int AnswerByExample(int int1, char @operator, int int2)
        {
            if (@operator == '+')
                return int1 + int2;
            else if (@operator == '-')
                return int1 - int2;
            else if (@operator == '*')
                return int1 * int2;
            else
                return int1 / int2;
        }
        public static object[] GetAnswerOptions(int rightAnswer, int tolerance)
        {
            var random = new Random();
            var random2 = random.Next(maxValue: tolerance + 1);
            var random3 = random.Next(maxValue: tolerance - 1);
            var randomO2 = GetRandomOperator();
            var randomO3 = GetRandomOperator();
            if (random2 == 0)
                random2 = 1;
            if (random3 == 0)
                random3 = 1;
            var answer1 = AnswerByExample(random2, randomO2, random3);
            var answer3 = AnswerByExample(random3, randomO3, random2);
            return new object[] { answer1, rightAnswer, answer3 };
        }
        public static object[] GetAnswerOptionsWithShifts(int rightAnswer, int shift)
        {
            return new object[] { rightAnswer - shift, rightAnswer, rightAnswer + shift };
        }
    }
}

// x, x-1, x+1
// x-2, x-1, x
// x, x+1, x+2

// (x-s+y), (x+y), (x+s+y)
// x = rightAnswer
// s = shift
// y = random { -shift, 0, +shift }
