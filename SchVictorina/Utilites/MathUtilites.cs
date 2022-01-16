using System;
using System.Collections.Generic;
using System.Text;

namespace SchVictorina.Utilites
{
    public class MathUtilites
    {
        public static char GetRandomOperator()
        {
            var randomizer = new Random();
            var randomInt = randomizer.Next(1, 5);
            return randomInt switch
            {
                1 => '+',
                2 => '-',
                3 => '*',
                _ => ':'
            };
        }
        public static char GetOperator(string example)
        {
            if (example.Contains("+"))
                return '+';
            else if (example.Contains("-"))
                return '-';
            else if (example.Contains("*"))
                return '*';
            else
                return ':';
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
        public static object[] GetAnswerOptions(int rightAnswer)
        {
            var random = new Random();
            var random2 = random.Next(maxValue: 6);
            var random3 = random.Next(maxValue: 6);
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
    }
}
